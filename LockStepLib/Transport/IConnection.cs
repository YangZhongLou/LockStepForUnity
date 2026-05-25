using System;

namespace LockStepLib.Transport
{
    /// <summary>
    /// 连接句柄抽象。每个已连接的对端对应一个 IConnection 实例。
    /// </summary>
    public interface IConnection
    {
        /// <summary>连接唯一标识 (由 Transport 分配)</summary>
        int Id { get; }

        /// <summary>连接是否存活</summary>
        bool IsConnected { get; }

        /// <summary>远端地址描述 (调试用)</summary>
        string RemoteEndPoint { get; }
    }
}
