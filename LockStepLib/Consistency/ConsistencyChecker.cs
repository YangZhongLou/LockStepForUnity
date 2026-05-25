using System.Collections.Generic;
using LockStepLib.Simulation;

namespace LockStepLib.Consistency
{
    /// <summary>
    /// 一致性校验工具。收集并比对帧哈希，检测不一致。
    /// </summary>
    public class ConsistencyChecker
    {
        private readonly int _checkInterval;
        private readonly Dictionary<int, Dictionary<int, ulong>> _hashes = new Dictionary<int, Dictionary<int, ulong>>(); // frame → playerId → hash

        /// <summary>最近检测到的不一致事件</summary>
        public DesyncInfo LastDesync { get; private set; }

        /// <summary>校验间隔 (帧数)</summary>
        public int CheckInterval => _checkInterval;

        public ConsistencyChecker(int checkInterval = 10)
        {
            _checkInterval = checkInterval;
        }

        /// <summary>是否为校验帧</summary>
        public bool IsCheckFrame(int frame) => _checkInterval > 0 && frame % _checkInterval == 0;

        /// <summary>记录本地哈希</summary>
        public void RecordLocalHash(int frame, int playerId, ulong hash)
        {
            if (!_hashes.ContainsKey(frame))
                _hashes[frame] = new Dictionary<int, ulong>();
            _hashes[frame][playerId] = hash;
        }

        /// <summary>记录远端哈希</summary>
        public void RecordRemoteHash(int frame, int playerId, ulong hash)
        {
            RecordLocalHash(frame, playerId, hash);
        }

        /// <summary>检查指定帧是否一致。返回不一致的玩家列表。</summary>
        public int[] CheckFrame(int frame)
        {
            if (!_hashes.TryGetValue(frame, out var playerHashes) || playerHashes.Count < 2)
                return System.Array.Empty<int>();

            ulong? expected = null;
            var mismatched = new List<int>();

            foreach (var kv in playerHashes)
            {
                if (!expected.HasValue)
                    expected = kv.Value;
                else if (kv.Value != expected.Value)
                    mismatched.Add(kv.Key);
            }

            if (mismatched.Count > 0)
            {
                LastDesync = new DesyncInfo
                {
                    Frame = frame,
                    LocalHash = expected ?? 0,
                    RemoteHash = playerHashes[mismatched[0]],
                    MismatchedPlayers = mismatched.ToArray(),
                };
                return mismatched.ToArray();
            }

            return System.Array.Empty<int>();
        }

        /// <summary>清理旧帧数据</summary>
        public void Trim(int keepFromFrame)
        {
            var toRemove = new List<int>();
            foreach (var kv in _hashes)
                if (kv.Key < keepFromFrame) toRemove.Add(kv.Key);
            foreach (var key in toRemove)
                _hashes.Remove(key);
        }

        /// <summary>重置</summary>
        public void Reset()
        {
            _hashes.Clear();
            LastDesync = null;
        }
    }
}
