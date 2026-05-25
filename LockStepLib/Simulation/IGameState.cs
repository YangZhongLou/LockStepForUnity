using System.IO;

namespace LockStepLib.Simulation
{
    /// <summary>
    /// 游戏状态序列化接口。用于观战重连的状态快照和 replay 初始状态保存。
    /// 由具体游戏实现，确保序列化/反序列化往返后完整恢复游戏状态。
    /// </summary>
    public interface IGameState
    {
        /// <summary>序列化到二进制写入器</summary>
        void Serialize(BinaryWriter writer);

        /// <summary>从二进制读取器反序列化</summary>
        void Deserialize(BinaryReader reader);
    }
}
