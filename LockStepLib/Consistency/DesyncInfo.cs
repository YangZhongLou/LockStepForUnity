namespace LockStepLib.Consistency
{
    /// <summary>
    /// 不一致事件信息
    /// </summary>
    public class DesyncInfo
    {
        /// <summary>不一致的帧号</summary>
        public int Frame { get; set; }

        /// <summary>本地计算的哈希</summary>
        public ulong LocalHash { get; set; }

        /// <summary>远端报告的哈希</summary>
        public ulong RemoteHash { get; set; }

        /// <summary>哪些玩家哈希不一致</summary>
        public int[] MismatchedPlayers { get; set; }
    }
}
