namespace LockStepLib.Command
{
    /// <summary>
    /// 确定性输入结构体。将玩家 ID、帧号和指令绑定在一起，
    /// 作为帧同步中每帧广播和缓冲的基本单位。
    /// </summary>
    public readonly struct DeterministicInput
    {
        /// <summary>玩家 ID (0-based)</summary>
        public readonly int PlayerId;

        /// <summary>目标帧号</summary>
        public readonly int Frame;

        /// <summary>该帧的输入指令</summary>
        public readonly IInputCommand Command;

        public DeterministicInput(int playerId, int frame, IInputCommand command)
        {
            PlayerId = playerId;
            Frame = frame;
            Command = command;
        }

        /// <summary>组合键，用于按 (Frame, PlayerId) 去重</summary>
        public long CompositeKey => ((long)Frame << 32) | (uint)PlayerId;

        public override string ToString()
        {
            return $"P{PlayerId}@F{Frame}: {Command?.GetType().Name ?? "null"}";
        }
    }
}
