namespace LockStepLib.Session
{
    /// <summary>
    /// 会话角色
    /// </summary>
    public enum SessionRole
    {
        /// <summary>服务器 — 收集输入、广播帧包、推进仿真</summary>
        Server,
        /// <summary>客户端 — 提交输入、接收帧包、同步仿真</summary>
        Client,
        /// <summary>观战者 — 只接收帧包，不提交输入</summary>
        Spectator,
    }

    /// <summary>
    /// 会话状态
    /// </summary>
    public enum SessionState
    {
        /// <summary>未启动</summary>
        Idle,
        /// <summary>正在连接 (客户端)</summary>
        Connecting,
        /// <summary>等待所有玩家就绪</summary>
        Synchronizing,
        /// <summary>正常运行中</summary>
        Running,
        /// <summary>已暂停</summary>
        Paused,
        /// <summary>已结束</summary>
        Finished,
    }

    /// <summary>
    /// 输入超时处理策略
    /// </summary>
    public enum MissingInputStrategy
    {
        /// <summary>等待所有玩家输入到齐 (可能阻塞)</summary>
        WaitAll,
        /// <summary>超时后对缺失玩家使用空指令</summary>
        UseDefault,
        /// <summary>超时后中止会话</summary>
        Abort,
    }
}
