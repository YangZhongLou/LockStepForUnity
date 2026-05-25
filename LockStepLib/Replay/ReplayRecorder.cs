using System;
using System.IO;
using LockStepLib.Command;
using LockStepLib.Core;

namespace LockStepLib.Replay
{
    /// <summary>
    /// 回放录制器。将帧包序列写入文件，同时写入元数据头。
    /// 文件格式: [Header: magic+gameId+version+config] [FramePackages...] [EOF: 0xFFFFFFFF]
    /// </summary>
    public class ReplayRecorder : IDisposable
    {
        private readonly CommandSerializer _serializer;
        private readonly ReplayMetadata _metadata;
        private FileStream _stream;
        private BinaryWriter _writer;

        public bool IsRecording => _stream != null;
        public string FilePath { get; private set; }
        public int RecordedFrames { get; private set; }

        public ReplayRecorder(CommandSerializer serializer, ReplayMetadata metadata)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>开始录制</summary>
        public void Start(string filePath)
        {
            FilePath = filePath;
            _stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new BinaryWriter(_stream);
            RecordedFrames = 0;

            // 写文件头
            _writer.Write(ReplayMetadata.MagicNumber);
            _writer.Write(ReplayMetadata.CurrentProtocolVersion);

            byte[] gameIdBytes = System.Text.Encoding.UTF8.GetBytes(_metadata.GameId ?? "");
            VarInt.WriteUInt32(_writer.BaseStream, (uint)gameIdBytes.Length);
            _writer.Write(gameIdBytes);

            _writer.Write(_metadata.GameVersion);
            _writer.Write(_metadata.FrameRate);
            _writer.Write(_metadata.PlayerCount);
            _writer.Write(_metadata.StartTime.Ticks);
        }

        /// <summary>录制一帧</summary>
        public void Record(FramePackage package)
        {
            if (!IsRecording) return;

            byte[] data = _serializer.SerializeFramePackage(package);

            _writer.Write(package.Frame);
            VarInt.WriteUInt32(_writer.BaseStream, (uint)data.Length);
            _writer.Write(data);
            RecordedFrames++;
        }

        /// <summary>结束录制</summary>
        public void Stop()
        {
            if (!IsRecording) return;

            // EOF 标记
            _writer.Write(-1); // frame = -1 = 0xFFFFFFFF
            _metadata.TotalFrames = RecordedFrames;

            _writer.Close();
            _stream.Close();
            _writer = null;
            _stream = null;
        }

        public void Dispose() => Stop();
    }
}
