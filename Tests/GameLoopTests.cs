using System;
using System.Threading;
using LockStepLib.Simulation;

namespace Tests
{
    public static class GameLoopTests
    {
        public static void Run()
        {
            Console.WriteLine("--- GameLoop Tests ---");

            StartStop();
            FrameAdvance();
            PauseResume();
            FastForward();
        }

        static void StartStop()
        {
            TestRunner.StartSection("Start/Stop");

            var loop = new GameLoop();
            TestRunner.AssertEqual(false, loop.IsRunning, "initially not running");
            TestRunner.AssertEqual(0, loop.CurrentFrame, "initial frame = 0");

            loop.Start(30);
            TestRunner.AssertEqual(true, loop.IsRunning, "started");
            TestRunner.AssertEqual(30, loop.FrameRate, "frame rate = 30");

            loop.Stop();
            TestRunner.AssertEqual(false, loop.IsRunning, "stopped");
            TestRunner.AssertEqual(0, loop.CurrentFrame, "frame reset to 0");
        }

        static void FrameAdvance()
        {
            TestRunner.StartSection("Frame Advance");

            var loop = new GameLoop();
            loop.Start(120); // high frame rate so we get frames quickly

            // 等待一段时间
            Thread.Sleep(50); // ~6 frames at 120fps

            int total = 0;
            for (int i = 0; i < 3; i++)
            {
                total += loop.Update();
                Thread.Sleep(5);
            }

            TestRunner.Assert(total > 0, $"advanced {total} frames (expected > 0)");

            loop.Stop();
        }

        static void PauseResume()
        {
            TestRunner.StartSection("Pause/Resume");

            var loop = new GameLoop();
            loop.Start(60);
            Thread.Sleep(20);

            int before = loop.Update();

            loop.Pause();
            TestRunner.AssertEqual(false, loop.IsRunning, "paused");

            Thread.Sleep(30);
            int pausedFrames = loop.Update();
            TestRunner.AssertEqual(0, pausedFrames, "no frames while paused");

            loop.Resume();
            TestRunner.AssertEqual(true, loop.IsRunning, "resumed");

            loop.Stop();
        }

        static void FastForward()
        {
            TestRunner.StartSection("Fast Forward");

            var loop = new GameLoop();
            loop.Start(30);

            int advanced = loop.FastForward(50);
            TestRunner.AssertEqual(50, advanced, "fast forwarded 50 frames");
            TestRunner.AssertEqual(50, loop.CurrentFrame, "current frame = 50");

            loop.Stop();
        }
    }
}
