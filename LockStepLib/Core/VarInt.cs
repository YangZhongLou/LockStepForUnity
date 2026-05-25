using System;
using System.IO;

namespace LockStepLib.Core
{
    /// <summary>
    /// 变长整数编解码 (类 Protocol Buffers VarInt)。
    /// 每字节低 7 位为数据，最高位为延续标志。
    /// 小值只需 1-2 字节，适合帧号、指令类型 ID 等。
    /// </summary>
    public static class VarInt
    {
        /// <summary>最大编码字节数 (ulong 最多 10 字节)</summary>
        private const int MAX_VARINT_BYTES = 10;

        #region Write

        /// <summary>编码 uint 到字节数组</summary>
        public static int WriteUInt32(byte[] buffer, int offset, uint value)
        {
            int start = offset;
            while (value > 0x7Fu)
            {
                buffer[offset++] = (byte)((value & 0x7Fu) | 0x80u);
                value >>= 7;
            }
            buffer[offset++] = (byte)value;
            return offset - start;
        }

        /// <summary>编码 ulong 到字节数组</summary>
        public static int WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            int start = offset;
            while (value > 0x7Ful)
            {
                buffer[offset++] = (byte)((value & 0x7Ful) | 0x80ul);
                value >>= 7;
            }
            buffer[offset++] = (byte)value;
            return offset - start;
        }

        /// <summary>编码 int (ZigZag: 将有符号映射到无符号)</summary>
        public static int WriteInt32(byte[] buffer, int offset, int value)
        {
            uint zigzag = (uint)((value << 1) ^ (value >> 31));
            return WriteUInt32(buffer, offset, zigzag);
        }

        /// <summary>编码 long (ZigZag)</summary>
        public static int WriteInt64(byte[] buffer, int offset, long value)
        {
            ulong zigzag = (ulong)((value << 1) ^ (value >> 63));
            return WriteUInt64(buffer, offset, zigzag);
        }

        #endregion

        #region Read

        /// <summary>从字节数组解码 uint</summary>
        public static uint ReadUInt32(byte[] buffer, ref int offset)
        {
            uint result = 0;
            int shift = 0;
            int maxBytes = 5; // uint 最多 5 字节
            while (offset < buffer.Length && maxBytes-- > 0)
            {
                byte b = buffer[offset++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
            throw new EndOfStreamException("VarInt 解码遇到意外结束");
        }

        /// <summary>从字节数组解码 ulong</summary>
        public static ulong ReadUInt64(byte[] buffer, ref int offset)
        {
            ulong result = 0;
            int shift = 0;
            int maxBytes = 10; // ulong 最多 10 字节
            while (offset < buffer.Length && maxBytes-- > 0)
            {
                byte b = buffer[offset++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
            throw new EndOfStreamException("VarInt 解码遇到意外结束");
        }

        /// <summary>解码 int (ZigZag 反向)</summary>
        public static int ReadInt32(byte[] buffer, ref int offset)
        {
            uint zigzag = ReadUInt32(buffer, ref offset);
            return (int)(zigzag >> 1) ^ -(int)(zigzag & 1);
        }

        /// <summary>解码 long (ZigZag 反向)</summary>
        public static long ReadInt64(byte[] buffer, ref int offset)
        {
            ulong zigzag = ReadUInt64(buffer, ref offset);
            return (long)(zigzag >> 1) ^ -(long)(zigzag & 1);
        }

        #endregion

        #region 流式 API

        /// <summary>编码 uint 到流</summary>
        public static void WriteUInt32(Stream stream, uint value)
        {
            while (value > 0x7Fu)
            {
                stream.WriteByte((byte)((value & 0x7Fu) | 0x80u));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }

        /// <summary>从流解码 uint</summary>
        public static uint ReadUInt32(Stream stream)
        {
            uint result = 0;
            int shift = 0;
            for (int i = 0; i < MAX_VARINT_BYTES; i++)
            {
                int b = stream.ReadByte();
                if (b < 0) throw new EndOfStreamException("VarInt 流读取意外结束");
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
            throw new FormatException("VarInt 超过最大字节数");
        }

        #endregion

        #region 长度查询

        /// <summary>计算 uint 编码后的字节数</summary>
        public static int GetUInt32Length(uint value)
        {
            int len = 1;
            while (value > 0x7Fu) { len++; value >>= 7; }
            return len;
        }

        /// <summary>计算 int (ZigZag) 编码后的字节数</summary>
        public static int GetInt32Length(int value)
        {
            uint zigzag = (uint)((value << 1) ^ (value >> 31));
            return GetUInt32Length(zigzag);
        }

        #endregion
    }
}
