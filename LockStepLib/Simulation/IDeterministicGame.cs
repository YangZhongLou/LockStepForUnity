using LockStepLib.Command;

namespace LockStepLib.Simulation
{
    /// <summary>
    /// 确定性游戏逻辑接口。用户实现此接口来定义游戏的具体行为。
    /// 所有方法必须在相同输入下产生完全相同的结果，不依赖平台相关特性。
    /// </summary>
    public interface IDeterministicGame
    {
        /// <summary>游戏唯一标识，用于校验 replay 兼容性</summary>
        string GameId { get; }

        /// <summary>游戏版本号，replay 文件匹配校验</summary>
        int GameVersion { get; }

        /// <summary>初始化游戏，传入初始状态</summary>
        void Initialize(IGameState initialState);

        /// <summary>
        /// 每帧更新游戏逻辑。
        /// inputs 包含所有玩家在当前帧的输入。
        /// </summary>
        void Update(DeterministicInput[] inputs, int frame);

        /// <summary>
        /// 获取当前帧的哈希值，用于跨端一致性校验。
        /// 返回值的计算方式由游戏决定，但相同游戏状态必须返回相同哈希。
        /// </summary>
        ulong GetFrameHash();

        /// <summary>获取当前完整游戏状态快照，用于观战/重连/回放</summary>
        IGameState GetStateSnapshot();

        /// <summary>从指定状态快照恢复游戏</summary>
        void RestoreState(IGameState state);
    }
}
