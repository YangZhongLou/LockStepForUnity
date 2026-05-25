using LockStepLib.Transport;

namespace LockStepLib.Session
{
    /// <summary>
    /// 玩家连接状态跟踪。
    /// </summary>
    public class PlayerConnection
    {
        public int PlayerId { get; }
        public IConnection Connection { get; }

        /// <summary>最后提交输入的帧号 (-1 表示未提交)</summary>
        public int LastInputFrame { get; set; } = -1;

        /// <summary>是否已断开</summary>
        public bool Disconnected { get; set; }

        public PlayerConnection(int playerId, IConnection connection)
        {
            PlayerId = playerId;
            Connection = connection;
        }
    }
}
