namespace LockStepLib.Command
{
    /// <summary>
    /// 帧广播包。包含当前帧所有玩家的输入，以及最近 K 帧的冗余输入。
    /// 冗余输入用于抵抗丢包：接收端按 (PlayerId, Frame) 去重，已有则忽略。
    /// </summary>
    public struct FramePackage
    {
        /// <summary>帧号</summary>
        public int Frame;

        /// <summary>当前帧所有玩家的输入</summary>
        public DeterministicInput[] PrimaryInputs;

        /// <summary>冗余帧的输入 (最近 K 帧)，可为 null</summary>
        public DeterministicInput[] RedundancyInputs;

        /// <summary>一致性校验数据 (每 K 帧附带一次)</summary>
        public ConsistencyData? Consistency;

        public FramePackage(int frame, DeterministicInput[] primary,
            DeterministicInput[] redundancy = null, ConsistencyData? consistency = null)
        {
            Frame = frame;
            PrimaryInputs = primary;
            RedundancyInputs = redundancy;
            Consistency = consistency;
        }
    }

    /// <summary>
    /// 一致性校验数据，随帧包附加传输。
    /// </summary>
    public struct ConsistencyData
    {
        /// <summary>校验帧号</summary>
        public int CheckFrame;

        /// <summary>本地计算的帧哈希</summary>
        public ulong FrameHash;

        public ConsistencyData(int checkFrame, ulong hash)
        {
            CheckFrame = checkFrame;
            FrameHash = hash;
        }
    }
}
