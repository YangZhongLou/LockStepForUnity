using System;
using System.Collections.Generic;

namespace LockStepLib.Command
{
    /// <summary>
    /// 输入指令缓冲。按 (PlayerId, Frame) 索引存储所有接收到的指令，
    /// 支持冗余输入去重、收集校验和过期帧清理。
    /// 线程安全（帧同步主线程操作，加锁为保险）。
    /// </summary>
    public class CommandBuffer
    {
        private readonly Dictionary<long, IInputCommand> _commands = new Dictionary<long, IInputCommand>();
        private readonly object _lock = new object();

        /// <summary>缓冲中的帧数</summary>
        public int Count
        {
            get { lock (_lock) return _commands.Count; }
        }

        /// <summary>添加或更新一条输入指令。相同 (PlayerId, Frame) 的重复输入会被忽略。</summary>
        /// <returns>true 表示新输入被接受，false 表示重复忽略</returns>
        public bool AddInput(int playerId, int frame, IInputCommand command)
        {
            long key = MakeKey(playerId, frame);
            lock (_lock)
            {
                if (_commands.ContainsKey(key)) return false;
                _commands[key] = command;
                return true;
            }
        }

        /// <summary>批量添加来自 FramePackage 的输入</summary>
        /// <returns>新接受的输入数量</returns>
        public int AddFramePackage(FramePackage package)
        {
            int accepted = 0;

            // 主输入
            if (package.PrimaryInputs != null)
            {
                foreach (var input in package.PrimaryInputs)
                {
                    if (AddInput(input.PlayerId, input.Frame, input.Command))
                        accepted++;
                }
            }

            // 冗余输入
            if (package.RedundancyInputs != null)
            {
                foreach (var input in package.RedundancyInputs)
                {
                    if (AddInput(input.PlayerId, input.Frame, input.Command))
                        accepted++;
                }
            }

            return accepted;
        }

        /// <summary>获取指定帧的所有玩家输入。玩家数不足时返回已有项。</summary>
        public DeterministicInput[] GetInputsForFrame(int frame, int expectedPlayerCount)
        {
            lock (_lock)
            {
                var result = new List<DeterministicInput>(expectedPlayerCount);
                for (int p = 0; p < expectedPlayerCount; p++)
                {
                    long key = MakeKey(p, frame);
                    if (_commands.TryGetValue(key, out var cmd))
                        result.Add(new DeterministicInput(p, frame, cmd));
                }
                return result.ToArray();
            }
        }

        /// <summary>检查指定帧的所有玩家输入是否到齐</summary>
        public bool AllPlayersReady(int frame, int expectedPlayerCount)
        {
            lock (_lock)
            {
                for (int p = 0; p < expectedPlayerCount; p++)
                {
                    long key = MakeKey(p, frame);
                    if (!_commands.ContainsKey(key))
                        return false;
                }
                return true;
            }
        }

        /// <summary>检查指定帧指定玩家的输入是否存在</summary>
        public bool HasInput(int playerId, int frame)
        {
            lock (_lock) return _commands.ContainsKey(MakeKey(playerId, frame));
        }

        /// <summary>获取指定玩家已提交的最新帧号。未提交过返回 -1。</summary>
        public int GetLatestFrameForPlayer(int playerId)
        {
            int latest = -1;
            lock (_lock)
            {
                foreach (var kv in _commands)
                {
                    int p = (int)(uint)(kv.Key & 0xFFFFFFFF);
                    int f = (int)(kv.Key >> 32);
                    if (p == playerId && f > latest)
                        latest = f;
                }
            }
            return latest;
        }

        /// <summary>清理小于指定帧号的所有输入</summary>
        public void Trim(int keepFromFrame)
        {
            lock (_lock)
            {
                var toRemove = new List<long>();
                foreach (var kv in _commands)
                {
                    int frame = (int)(kv.Key >> 32);
                    if (frame < keepFromFrame)
                        toRemove.Add(kv.Key);
                }
                foreach (var key in toRemove)
                    _commands.Remove(key);
            }
        }

        /// <summary>清空缓冲</summary>
        public void Clear()
        {
            lock (_lock) _commands.Clear();
        }

        private static long MakeKey(int playerId, int frame)
        {
            return ((long)frame << 32) | (uint)playerId;
        }
    }
}
