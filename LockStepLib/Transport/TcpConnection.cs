using System;
using System.Net.Sockets;

namespace LockStepLib.Transport
{
    /// <summary>
    /// TCP 连接封装。包装 TcpClient，提供带长度前缀的帧收发。
    /// 消息格式: [4-byte length (int, little-endian)][payload bytes]
    /// </summary>
    internal class TcpConnection : IConnection
    {
        private static int _nextId;

        public int Id { get; }
        public bool IsConnected => _tcpClient?.Connected ?? false;
        public string RemoteEndPoint { get; }

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly TcpTransport _transport;
        private readonly object _sendLock = new object();

        // 接收状态
        private readonly byte[] _lengthBuf = new byte[4];
        private int _lengthRead;
        private byte[] _payloadBuf;
        private int _payloadRead;
        private bool _readingLength = true;

        /// <summary>挂起的接收错误 (在 Update 时报告)</summary>
        public string PendingError { get; private set; }

        internal TcpConnection(TcpClient client, TcpTransport transport)
        {
            Id = System.Threading.Interlocked.Increment(ref _nextId);
            _tcpClient = client;
            _stream = client.GetStream();
            _transport = transport;
            RemoteEndPoint = client.Client?.RemoteEndPoint?.ToString() ?? "unknown";
        }

        /// <summary>开始异步接收</summary>
        internal void BeginReceive()
        {
            try
            {
                _stream.BeginRead(_lengthBuf, 0, 4, OnReceive, null);
            }
            catch (Exception ex)
            {
                PendingError = ex.Message;
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _stream.EndRead(ar);
                if (bytesRead == 0)
                {
                    PendingError = "connection closed";
                    return;
                }

                if (_readingLength)
                {
                    _lengthRead += bytesRead;
                    if (_lengthRead < 4)
                    {
                        // 继续读长度头
                        _stream.BeginRead(_lengthBuf, _lengthRead, 4 - _lengthRead, OnReceive, null);
                        return;
                    }

                    // 长度头读完，开始读负载
                    int payloadLen = BitConverter.ToInt32(_lengthBuf, 0);
                    if (payloadLen <= 0 || payloadLen > 1024 * 1024) // 最大 1MB
                    {
                        PendingError = $"invalid payload length: {payloadLen}";
                        return;
                    }

                    _payloadBuf = new byte[payloadLen];
                    _payloadRead = 0;
                    _readingLength = false;
                    _stream.BeginRead(_payloadBuf, 0, payloadLen, OnReceive, null);
                }
                else
                {
                    _payloadRead += bytesRead;
                    if (_payloadRead < _payloadBuf.Length)
                    {
                        // 继续读负载
                        _stream.BeginRead(_payloadBuf, _payloadRead, _payloadBuf.Length - _payloadRead, OnReceive, null);
                        return;
                    }

                    // 完整消息到达，放入接收队列
                    var data = _payloadBuf;
                    _payloadBuf = null;
                    _readingLength = true;
                    _lengthRead = 0;

                    _transport.EnqueueData(this, data);

                    // 开始读下一帧
                    _stream.BeginRead(_lengthBuf, 0, 4, OnReceive, null);
                }
            }
            catch (Exception ex)
            {
                PendingError = ex.Message;
            }
        }

        /// <summary>发送数据 (线程安全)</summary>
        internal bool Send(byte[] data, int offset, int length)
        {
            lock (_sendLock)
            {
                try
                {
                    // 写长度前缀 + 数据
                    byte[] lengthPrefix = BitConverter.GetBytes(length);
                    _stream.Write(lengthPrefix, 0, 4);
                    _stream.Write(data, offset, length);
                    _stream.Flush();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal void Close()
        {
            try { _stream.Close(); } catch { }
            try { _tcpClient.Close(); } catch { }
        }
    }
}
