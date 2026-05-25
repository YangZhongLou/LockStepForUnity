namespace LockStepLib.Transport
{
    /// <summary>
    /// 发送通道类型
    /// </summary>
    public enum SendChannel
    {
        /// <summary>可靠有序 (TCP 默认)</summary>
        Reliable = 0,

        /// <summary>不可靠 (UDP 场景，TCP 实现中退化为 Reliable)</summary>
        Unreliable = 1,
    }

    /// <summary>
    /// 发送选项
    /// </summary>
    public struct SendOptions
    {
        public SendChannel Channel;

        public static readonly SendOptions Reliable = new SendOptions { Channel = SendChannel.Reliable };
        public static readonly SendOptions Unreliable = new SendOptions { Channel = SendChannel.Unreliable };
    }
}
