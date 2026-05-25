using System;

namespace LockStepLib.Replay
{
    /// <summary>
    /// 回放文件元数据 (文件头)
    /// </summary>
    public class ReplayMetadata
    {
        /// <summary>魔数 "LSRP" = 0x4C535250</summary>
        public const uint MagicNumber = 0x4C535250;

        /// <summary>当前协议版本</summary>
        public const int CurrentProtocolVersion = 1;

        /// <summary>游戏标识</summary>
        public string GameId { get; set; }

        /// <summary>游戏版本</summary>
        public int GameVersion { get; set; }

        /// <summary>帧率</summary>
        public int FrameRate { get; set; }

        /// <summary>玩家数量</summary>
        public int PlayerCount { get; set; }

        /// <summary>录制起始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>总帧数</summary>
        public int TotalFrames { get; set; }

        /// <summary>文件格式: "LSRP" + v{VERSION}</summary>
        public string FileFormat => $"LSRP v{CurrentProtocolVersion}";
    }
}
