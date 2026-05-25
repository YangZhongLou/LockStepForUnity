using System;

namespace LockStepLib.Transport
{
    /// <summary>
    /// 网络传输层抽象接口。支持插件式替换 (TCP/UDP/自定义)。
    /// 所有网络 I/O 在内部线程执行，事件通过 Update() 在主线程回调。
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>新连接建立 (服务器端)</summary>
        event Action<IConnection> OnConnected;

        /// <summary>连接断开</summary>
        event Action<IConnection, string> OnDisconnected;

        /// <summary>收到数据</summary>
        event Action<IConnection, ArraySegment<byte>> OnDataReceived;

        /// <summary>启动服务器，监听指定端口</summary>
        void StartServer(int port, int maxConnections);

        /// <summary>以客户端模式连接到远程服务器</summary>
        void Connect(string host, int port);

        /// <summary>断开指定连接</summary>
        void Disconnect(IConnection connection);

        /// <summary>断开所有连接并停止监听</summary>
        void Shutdown();

        /// <summary>向指定连接发送数据</summary>
        void Send(IConnection conn, byte[] data, int offset, int length, SendOptions options);

        /// <summary>向所有已连接客户端广播数据</summary>
        void Broadcast(byte[] data, int offset, int length, SendOptions options, IConnection exclude = null);

        /// <summary>处理网络事件队列，需在主线程每帧调用</summary>
        void Update();

        /// <summary>当前连接数 (不含监听器)</summary>
        int ConnectionCount { get; }

        /// <summary>是否为服务器模式</summary>
        bool IsServer { get; }
    }
}
