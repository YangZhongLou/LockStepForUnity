using System;
using System.Collections.Generic;
using System.IO;
using LockStepLib.Command;
using LockStepLib.Core;

namespace LockStepLib.Replay
{
    /// <summary>
    /// 回放播放器。从文件读取帧包序列，支持按帧号查询和顺序播放。
    /// </summary>
    public class ReplayPlayer
    {
        private readonly CommandSerializer _serializer;
        private readonly List<FramePackage> _frames = new List<FramePackage>();
        private int _playbackIndex;

        /// <summary>回放元数据</summary>
        public ReplayMetadata Metadata { get; } = new ReplayMetadata();

        /// <summary>总帧数</summary>
        public int TotalFrames => _frames.Count;

        /// <summary>当前播放位置</summary>
        public int CurrentFrame { get; private set; }

        /// <summary>是否播放完毕</summary>
        public bool IsFinished => _playbackIndex >= _frames.Count;

        public ReplayPlayer(CommandSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>加载回放文件</summary>
        public bool Load(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    // 读头
                    uint magic = reader.ReadUInt32();
                    if (magic != ReplayMetadata.MagicNumber)
                        throw new FormatException($"无效的回放文件魔数: 0x{magic:X8}");

                    int protocolVer = reader.ReadInt32();

                    int gameIdLen = (int)VarInt.ReadUInt32(reader.BaseStream);
                    Metadata.GameId = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(gameIdLen));
                    Metadata.GameVersion = reader.ReadInt32();
                    Metadata.FrameRate = reader.ReadInt32();
                    Metadata.PlayerCount = reader.ReadInt32();
                    Metadata.StartTime = new DateTime(reader.ReadInt64());

                    // 读帧
                    _frames.Clear();
                    while (true)
                    {
                        int frame = reader.ReadInt32();
                        if (frame == -1) break; // EOF

                        int dataLen = (int)VarInt.ReadUInt32(reader.BaseStream);
                        byte[] data = reader.ReadBytes(dataLen);
                        var pkg = _serializer.DeserializeFramePackage(data);
                        _frames.Add(pkg);
                    }

                    Metadata.TotalFrames = _frames.Count;
                }

                Reset();
                return true;
            }
            catch (Exception)
            {
                _frames.Clear();
                return false;
            }
        }

        /// <summary>获取指定帧的帧包</summary>
        public FramePackage? GetFrame(int frame)
        {
            foreach (var pkg in _frames)
                if (pkg.Frame == frame) return pkg;
            return null;
        }

        /// <summary>获取下一帧 (顺序播放)</summary>
        public FramePackage? GetNext()
        {
            if (IsFinished) return null;
            var pkg = _frames[_playbackIndex];
            _playbackIndex++;
            CurrentFrame = pkg.Frame;
            return pkg;
        }

        /// <summary>重置播放位置</summary>
        public void Reset()
        {
            _playbackIndex = 0;
            CurrentFrame = 0;
        }
    }
}
