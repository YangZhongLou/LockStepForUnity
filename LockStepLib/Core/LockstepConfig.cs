namespace LockStepLib.Core
{
    /// <summary>
    /// 帧同步系统全局配置常量。运行时不可修改的值集中于此。
    /// </summary>
    public static class LockstepConfig
    {
        /// <summary>协议版本号，用于校验客户端/服务器兼容性</summary>
        public const int ProtocolVersion = 1;

        /// <summary>默认 TCP 端口</summary>
        public const int DefaultPort = 9550;

        /// <summary>最大同时连接数</summary>
        public const int MaxConnections = 32;

        /// <summary>单帧最大输入大小 (字节)，超出则截断或报错</summary>
        public const int MaxInputSizePerFrame = 512;

        /// <summary>FramePackage 最大序列化大小 (字节)</summary>
        public const int MaxFramePackageSize = 4096;

        /// <summary>接收缓冲区大小</summary>
        public const int ReceiveBufferSize = 8192;

        /// <summary>网络事件队列初始容量</summary>
        public const int InitialEventQueueCapacity = 64;

        /// <summary>网络轮询间隔 (ms)，驱动 INetworkTransport.Update()</summary>
        public const int NetworkPollIntervalMs = 5;

        /// <summary>帧缓冲默认大小</summary>
        public const int DefaultFrameBufferSize = 3;

        /// <summary>帧缓冲最小值 (低于此值暂停推进)</summary>
        public const int MinFrameBufferSize = 1;

        /// <summary>帧缓冲最大值 (动态调整上限)</summary>
        public const int MaxFrameBufferSize = 8;

        /// <summary>默认输入超时 (ms)，超时后按 MissingInputStrategy 处理</summary>
        public const int DefaultInputTimeoutMs = 5000;

        /// <summary>一致性校验间隔 (帧数)</summary>
        public const int DefaultConsistencyCheckInterval = 10;

        /// <summary>状态快照间隔 (帧数)，用于观战/重连</summary>
        public const int DefaultStateSnapshotInterval = 30;

        /// <summary>状态快照环形缓冲容量</summary>
        public const int StateSnapshotRingCapacity = 64;

        /// <summary>输入冗余帧数 (每包携带最近 K 帧的指令)</summary>
        public const int InputRedundancyFrames = 3;

        /// <summary>回放文件扩展名</summary>
        public const string ReplayFileExtension = ".lsrp";

        /// <summary>回放文件魔数</summary>
        public const uint ReplayMagicNumber = 0x4C535250; // "LSRP"

        /// <summary>快进时每 Update 模拟的帧数</summary>
        public const int FastForwardFramesPerUpdate = 10;
    }
}
