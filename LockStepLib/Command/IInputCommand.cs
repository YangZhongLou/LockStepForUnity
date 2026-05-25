using System.IO;

namespace LockStepLib.Command
{
    /// <summary>
    /// 游戏输入指令接口。由具体游戏实现，定义每帧玩家的操作数据。
    /// 实现类必须是纯数据的、可序列化的，且不依赖任何外部状态。
    /// </summary>
    public interface IInputCommand
    {
        /// <summary>指令类型 ID，全局唯一。用于序列化时的类型识别。</summary>
        int CommandTypeId { get; }

        /// <summary>序列化到二进制写入器</summary>
        void Serialize(BinaryWriter writer);

        /// <summary>从二进制读取器反序列化</summary>
        void Deserialize(BinaryReader reader);
    }
}
