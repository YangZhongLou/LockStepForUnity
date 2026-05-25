using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LockStepLib.Transport
{
    /// <summary>
    /// TCP 传输实现。基于 TcpListener/TcpClient，长度前缀帧协议。
    /// 后台线程处理 accept 和 receive，主线程通过 Update() 派发事件。
    /// </summary>
    public class TcpTransport : INetworkTransport, IDisposable
    {
        public event Action<IConnection> OnConnected;
        public event Action<IConnection, string> OnDisconnected;
        public event Action<IConnection, ArraySegment<byte>> OnDataReceived;

        public int ConnectionCount
        {
            get { lock (_connections) return _connections.Count; }
        }

        public bool IsServer => _listener != null;

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        private readonly List<TcpConnection> _connections = new List<TcpConnection>();
        private readonly ConcurrentQueue<TransportEvent> _eventQueue = new ConcurrentQueue<TransportEvent>();

        #region 服务器 / 客户端启动

        public void StartServer(int port, int maxConnections)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "TcpTransport-Accept"
            };
            _acceptThread.Start();
        }

        public void Connect(string host, int port)
        {
            var client = new TcpClient();
            client.BeginConnect(host, port, OnClientConnected, client);
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            try
            {
                var client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);
                var conn = AddConnection(client);
                _eventQueue.Enqueue(new TransportEvent
                {
                    Type = TransportEventType.Connected,
                    Connection = conn,
                });
                conn.BeginReceive();
            }
            catch (Exception ex)
            {
                _eventQueue.Enqueue(new TransportEvent
                {
                    Type = TransportEventType.Disconnected,
                    Reason = ex.Message,
                });
            }
        }

        #endregion

        #region 连接管理

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    var conn = AddConnection(client);
                    _eventQueue.Enqueue(new TransportEvent
                    {
                        Type = TransportEventType.Connected,
                        Connection = conn,
                    });
                    conn.BeginReceive();
                }
                catch (SocketException)
                {
                    if (!_running) break; // 主动停止 → 退出
                    // 瞬时错误 → 继续接受
                }
                catch (Exception)
                {
                }
            }
        }

        private TcpConnection AddConnection(TcpClient client)
        {
            var conn = new TcpConnection(client, this);
            lock (_connections)
            {
                _connections.Add(conn);
            }
            return conn;
        }

        internal void EnqueueData(TcpConnection conn, byte[] data)
        {
            _eventQueue.Enqueue(new TransportEvent
            {
                Type = TransportEventType.DataReceived,
                Connection = conn,
                Data = data,
            });
        }

        #endregion

        #region 发送

        public void Send(IConnection conn, byte[] data, int offset, int length, SendOptions options)
        {
            if (conn is TcpConnection tcpConn)
            {
                if (!tcpConn.Send(data, offset, length))
                {
                    EnqueueDisconnect(tcpConn, "send failed");
                }
            }
        }

        public void Broadcast(byte[] data, int offset, int length, SendOptions options, IConnection exclude = null)
        {
            lock (_connections)
            {
                foreach (var conn in _connections)
                {
                    if (conn == exclude) continue;
                    if (!conn.IsConnected) continue;
                    conn.Send(data, offset, length);
                }
            }
        }

        #endregion

        #region 断开

        public void Disconnect(IConnection connection)
        {
            if (connection is TcpConnection tcpConn)
            {
                tcpConn.Close();
                RemoveConnection(tcpConn);
            }
        }

        private void EnqueueDisconnect(TcpConnection conn, string reason)
        {
            _eventQueue.Enqueue(new TransportEvent
            {
                Type = TransportEventType.Disconnected,
                Connection = conn,
                Reason = reason,
            });
        }

        private void RemoveConnection(TcpConnection conn)
        {
            lock (_connections)
            {
                _connections.Remove(conn);
            }
        }

        #endregion

        #region Update / Shutdown

        public void Update()
        {
            int processed = 0;
            while (processed < 256 && _eventQueue.TryDequeue(out var evt))
            {
                processed++;
                switch (evt.Type)
                {
                    case TransportEventType.Connected:
                        OnConnected?.Invoke(evt.Connection);
                        break;
                    case TransportEventType.Disconnected:
                        if (evt.Connection is TcpConnection tc)
                        {
                            tc.Close();
                            RemoveConnection(tc);
                        }
                        OnDisconnected?.Invoke(evt.Connection, evt.Reason);
                        break;
                    case TransportEventType.DataReceived:
                        OnDataReceived?.Invoke(evt.Connection, new ArraySegment<byte>(evt.Data));
                        break;
                }
            }

            // 检查连接错误
            lock (_connections)
            {
                for (int i = _connections.Count - 1; i >= 0; i--)
                {
                    var conn = _connections[i];
                    if (conn.PendingError != null)
                    {
                        string err = conn.PendingError;
                        conn.Close();
                        _connections.RemoveAt(i);
                        OnDisconnected?.Invoke(conn, err);
                    }
                }
            }
        }

        public void Shutdown()
        {
            _running = false;
            _listener?.Stop();

            lock (_connections)
            {
                foreach (var conn in _connections)
                    conn.Close();
                _connections.Clear();
            }

            OnConnected = null;
            OnDisconnected = null;
            OnDataReceived = null;
        }

        public void Dispose()
        {
            Shutdown();
        }

        #endregion
    }
}
