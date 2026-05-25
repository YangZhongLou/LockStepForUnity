using System.Diagnostics;
using LockStepLib.Math;

namespace LockStepLib.Simulation
{
    /// <summary>
    /// 帧率控制器。基于 Stopwatch 的累加器模式，
    /// 确保帧推进速率与配置的帧率一致。支持快进模式。
    /// </summary>
    public class GameLoop
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _targetFrameTicks;
        private long _accumulator;
        private int _currentFrame;
        private bool _running;

        /// <summary>当前帧号</summary>
        public int CurrentFrame => _currentFrame;

        /// <summary>固定帧间隔 (秒)</summary>
        public Fix64 DeltaTime { get; private set; }

        /// <summary>目标帧率</summary>
        public int FrameRate { get; private set; }

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _running;

        /// <summary>
        /// 启动帧循环
        /// </summary>
        /// <param name="frameRate">目标帧率 (1-120)</param>
        public void Start(int frameRate)
        {
            FrameRate = frameRate > 0 ? frameRate : 30;
            _targetFrameTicks = Stopwatch.Frequency / FrameRate;
            DeltaTime = Fix64.One / Fix64.FromInt(FrameRate);
            _accumulator = 0;
            _currentFrame = 0;
            _stopwatch.Restart();
            _running = true;
        }

        /// <summary>
        /// 每帧调用，返回应该推进的帧数。
        /// </summary>
        /// <returns>需要模拟的帧数 (正常为 0 或 1，快进时 > 1)</returns>
        public int Update()
        {
            if (!_running) return 0;

            long elapsed = _stopwatch.ElapsedTicks;
            _stopwatch.Restart();
            _accumulator += elapsed;

            int framesToAdvance = 0;
            while (_accumulator >= _targetFrameTicks)
            {
                _accumulator -= _targetFrameTicks;
                framesToAdvance++;
                _currentFrame++;
            }

            // 防止螺旋: 如果落后太多, 限制单次最大帧数
            if (framesToAdvance > 20)
            {
                _accumulator = 0;
                framesToAdvance = 20;
            }

            return framesToAdvance;
        }

        /// <summary>
        /// 强制推进指定帧数 (用于快进 replay)
        /// </summary>
        public int FastForward(int frameCount)
        {
            _currentFrame += frameCount;
            _accumulator = 0;
            return frameCount;
        }

        /// <summary>暂停帧推进</summary>
        public void Pause()
        {
            _running = false;
            _stopwatch.Stop();
        }

        /// <summary>恢复帧推进</summary>
        public void Resume()
        {
            _running = true;
            _stopwatch.Restart();
        }

        /// <summary>停止并重置</summary>
        public void Stop()
        {
            _running = false;
            _stopwatch.Stop();
            _currentFrame = 0;
            _accumulator = 0;
        }
    }
}
