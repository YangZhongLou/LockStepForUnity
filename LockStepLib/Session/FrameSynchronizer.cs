using System.Collections.Generic;
using LockStepLib.Command;

namespace LockStepLib.Session
{
    /// <summary>
    /// 帧同步器。管理客户端帧包缓冲，控制帧推进时机。
    /// 支持动态缓冲区大小调整以应对网络抖动。
    /// </summary>
    public class FrameSynchronizer
    {
        private readonly int _targetBufferSize;
        private readonly SortedDictionary<int, FramePackage> _buffer = new SortedDictionary<int, FramePackage>();

        /// <summary>当前缓冲区中的帧数</summary>
        public int BufferedCount => _buffer.Count;

        /// <summary>缓冲区目标大小</summary>
        public int TargetBufferSize { get; private set; }

        /// <summary>下一个应该推进的帧号</summary>
        public int NextFrame { get; private set; }

        /// <summary>是否饥饿 (缓冲区耗尽，无法推进)</summary>
        public bool IsStarving => BufferedCount == 0;

        /// <summary>总饥饿帧数 (统计用)</summary>
        public int StarvationFrames { get; private set; }

        public FrameSynchronizer(int bufferSize = 3)
        {
            _targetBufferSize = bufferSize;
            TargetBufferSize = bufferSize;
            NextFrame = 0;
        }

        /// <summary>加入帧包到缓冲区。已存在的帧忽略。</summary>
        public void Enqueue(FramePackage package)
        {
            if (!_buffer.ContainsKey(package.Frame))
                _buffer[package.Frame] = package;
        }

        /// <summary>检查是否应该推进下一帧</summary>
        public bool ShouldAdvance()
        {
            if (_buffer.Count == 0) return false;
            // 缓冲区达到目标大小，或者已经有当前帧
            return _buffer.Count >= TargetBufferSize || _buffer.ContainsKey(NextFrame);
        }

        /// <summary>获取下一帧并推进指针。调用前确保 ShouldAdvance 为 true。</summary>
        public FramePackage? Dequeue()
        {
            if (!_buffer.TryGetValue(NextFrame, out var pkg)) return null;
            _buffer.Remove(NextFrame);
            NextFrame++;

            // 饥饿检测
            if (_buffer.Count == 0) StarvationFrames++;

            return pkg;
        }

        /// <summary>调整缓冲区目标大小</summary>
        public void AdjustBufferSize(int delta)
        {
            TargetBufferSize += delta;
            if (TargetBufferSize < 1) TargetBufferSize = 1;
            if (TargetBufferSize > 8) TargetBufferSize = 8;
        }

        /// <summary>动态缓冲区调整：饥饿时增加，积压时减少</summary>
        public void DynamicAdjust()
        {
            if (_buffer.Count == 0)
            {
                // 饥饿 → 增大缓冲
                if (TargetBufferSize < 6)
                    TargetBufferSize++;
            }
            else if (_buffer.Count > TargetBufferSize + 3)
            {
                // 积压严重 → 减小缓冲 (同时快进消费一些帧)
                if (TargetBufferSize > 2)
                    TargetBufferSize--;
            }
        }

        /// <summary>跳过指定帧号 (快进用)</summary>
        public void SkipToFrame(int frame)
        {
            while (NextFrame < frame && _buffer.Count > 0)
            {
                _buffer.Remove(NextFrame);
                NextFrame++;
            }
            if (NextFrame < frame) NextFrame = frame;
        }

        /// <summary>重置到初始状态</summary>
        public void Reset()
        {
            _buffer.Clear();
            NextFrame = 0;
            TargetBufferSize = _targetBufferSize;
            StarvationFrames = 0;
        }
    }
}
