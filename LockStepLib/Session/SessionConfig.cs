namespace LockStepLib.Session
{
    /// <summary>
    /// 会话配置。构造后通过属性设置，启动前可修改。
    /// </summary>
    public class SessionConfig
    {
        /// <summary>目标帧率 (1-120)，默认 30</summary>
        public int FrameRate { get; set; } = 30;

        /// <summary>预期玩家数量 (含本地)</summary>
        public int PlayerCount { get; set; } = 2;

        /// <summary>本地玩家 ID</summary>
        public int LocalPlayerId { get; set; } = 0;

        /// <summary>网络端口</summary>
        public int Port { get; set; } = 9550;

        /// <summary>服务器地址 (客户端模式)</summary>
        public string ServerHost { get; set; } = "127.0.0.1";

        /// <summary>帧缓冲大小 (客户端)，默认 3</summary>
        public int FrameBufferSize { get; set; } = 3;

        /// <summary>输入冗余帧数 (每包携带最近 K 帧)</summary>
        public int InputRedundancy { get; set; } = 3;

        /// <summary>一致性校验间隔 (帧数)，默认 10</summary>
        public int ConsistencyCheckInterval { get; set; } = 10;

        /// <summary>状态快照间隔 (帧数)，默认 30</summary>
        public int StateSnapshotInterval { get; set; } = 30;

        /// <summary>输入超时 (ms)，默认 5000</summary>
        public int InputTimeoutMs { get; set; } = 5000;

        /// <summary>输入超时处理策略</summary>
        public MissingInputStrategy InputStrategy { get; set; } = MissingInputStrategy.WaitAll;
    }
}
