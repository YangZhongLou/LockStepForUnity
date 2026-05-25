using System;
using System.IO;
using LockStepLib.Core;

namespace LockStepLib.Command
{
    /// <summary>
    /// 指令二进制序列化器。使用 VarInt 紧凑编码，支持批量序列化和反序列化。
    /// 需要在反序列化前通过 CommandFactory 注册所有指令类型。
    /// </summary>
    public class CommandSerializer
    {
        /// <summary>指令工厂，外部可注册类型</summary>
        public CommandFactory Factory { get; }

        public CommandSerializer(CommandFactory factory)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        #region 单指令序列化

        /// <summary>序列化单个输入到字节数组</summary>
        public byte[] SerializeInput(DeterministicInput input)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteInput(writer, input);
                return ms.ToArray();
            }
        }

        /// <summary>反序列化单个输入</summary>
        public DeterministicInput DeserializeInput(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                return ReadInput(reader);
            }
        }

        #endregion

        #region 批量序列化

        /// <summary>批量序列化输入数组</summary>
        public byte[] SerializeInputs(DeterministicInput[] inputs)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteInputArray(writer, inputs);
                return ms.ToArray();
            }
        }

        /// <summary>批量反序列化</summary>
        public DeterministicInput[] DeserializeInputs(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                return ReadInputArray(reader);
            }
        }

        #endregion

        #region FramePackage 序列化

        /// <summary>序列化帧包</summary>
        public byte[] SerializeFramePackage(FramePackage package)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                VarInt.WriteUInt32(writer.BaseStream, (uint)package.Frame);

                // 主输入
                WriteInputArray(writer, package.PrimaryInputs);

                // 冗余输入: 先写数量，再写数据
                int redundancyCount = package.RedundancyInputs?.Length ?? 0;
                VarInt.WriteUInt32(writer.BaseStream, (uint)redundancyCount);
                if (redundancyCount > 0)
                    WriteInputArray(writer, package.RedundancyInputs);

                // 一致性数据 (可选)
                bool hasConsistency = package.Consistency.HasValue;
                writer.Write(hasConsistency);
                if (hasConsistency)
                {
                    var c = package.Consistency.Value;
                    VarInt.WriteUInt32(writer.BaseStream, (uint)c.CheckFrame);
                    writer.Write(c.FrameHash);
                }

                return ms.ToArray();
            }
        }

        /// <summary>反序列化帧包</summary>
        public FramePackage DeserializeFramePackage(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                int frame = (int)VarInt.ReadUInt32(reader.BaseStream);

                // 主输入
                var primary = ReadInputArray(reader);

                // 冗余输入
                int redundancyCount = (int)VarInt.ReadUInt32(reader.BaseStream);
                var redundancy = redundancyCount > 0 ? ReadInputArray(reader) : null;

                // 一致性数据
                ConsistencyData? consistency = null;
                bool hasConsistency = reader.ReadBoolean();
                if (hasConsistency)
                {
                    int checkFrame = (int)VarInt.ReadUInt32(reader.BaseStream);
                    ulong hash = reader.ReadUInt64();
                    consistency = new ConsistencyData(checkFrame, hash);
                }

                return new FramePackage(frame, primary, redundancy, consistency);
            }
        }

        #endregion

        #region 内部辅助

        private void WriteInput(BinaryWriter writer, DeterministicInput input)
        {
            VarInt.WriteUInt32(writer.BaseStream, (uint)input.PlayerId);
            VarInt.WriteUInt32(writer.BaseStream, (uint)input.Frame);
            VarInt.WriteUInt32(writer.BaseStream, (uint)input.Command.CommandTypeId);

            // 指令数据: 先序列化到临时 buffer 以获取长度
            byte[] cmdData = SerializeCommand(input.Command);
            VarInt.WriteUInt32(writer.BaseStream, (uint)cmdData.Length);
            writer.Write(cmdData);
        }

        private DeterministicInput ReadInput(BinaryReader reader)
        {
            int playerId = (int)VarInt.ReadUInt32(reader.BaseStream);
            int frame = (int)VarInt.ReadUInt32(reader.BaseStream);
            int typeId = (int)VarInt.ReadUInt32(reader.BaseStream);
            int dataLen = (int)VarInt.ReadUInt32(reader.BaseStream);

            IInputCommand cmd = Factory.Create(typeId);
            byte[] cmdData = reader.ReadBytes(dataLen);
            using (var cmdMs = new MemoryStream(cmdData))
            using (var cmdReader = new BinaryReader(cmdMs))
            {
                cmd.Deserialize(cmdReader);
            }

            return new DeterministicInput(playerId, frame, cmd);
        }

        private void WriteInputArray(BinaryWriter writer, DeterministicInput[] inputs)
        {
            VarInt.WriteUInt32(writer.BaseStream, (uint)(inputs?.Length ?? 0));
            if (inputs == null) return;
            foreach (var input in inputs)
                WriteInput(writer, input);
        }

        private DeterministicInput[] ReadInputArray(BinaryReader reader)
        {
            int count = (int)VarInt.ReadUInt32(reader.BaseStream);
            if (count < 0 || count > 2048) // 单帧最大 2048 条输入
                throw new FormatException($"输入数量超限: {count}");
            var result = new DeterministicInput[count];
            for (int i = 0; i < count; i++)
                result[i] = ReadInput(reader);
            return result;
        }

        private byte[] SerializeCommand(IInputCommand cmd)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                cmd.Serialize(writer);
                return ms.ToArray();
            }
        }

        #endregion
    }
}
