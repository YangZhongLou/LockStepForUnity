using System;

namespace LockStepLib.Transport
{
    /// <summary>
    /// 网络事件类型
    /// </summary>
    public enum TransportEventType
    {
        Connected,
        Disconnected,
        DataReceived,
    }

    /// <summary>
    /// 网络事件。由后台线程生成，主线程通过 Update() 消费。
    /// </summary>
    public struct TransportEvent
    {
        /// <summary>事件类型</summary>
        public TransportEventType Type;

        /// <summary>关联的连接</summary>
        public IConnection Connection;

        /// <summary>接收到的数据 (仅 DataReceived 事件有效)</summary>
        public byte[] Data;

        /// <summary>断开原因 (仅 Disconnected 事件有效)</summary>
        public string Reason;
    }
}
