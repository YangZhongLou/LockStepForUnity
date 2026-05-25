using System;
using LockStepLib.Core;

namespace Tests
{
    class Program
    {
        static int Main()
        {
            LogManager.InitializeConsole();
            Console.WriteLine("=== LockStepLib Tests ===\n");

            // P1: Math
            Fix64Tests.Run();
            FixMathTests.Run();
            FixVector2Tests.Run();
            FixVector3Tests.Run();
            FixMatrixTests.Run();

            // P2: Core
            VarIntTests.Run();
            ByteArrayPoolTests.Run();

            // P3: Command
            CommandSerializerTests.Run();
            CommandBufferTests.Run();

            // P4: Transport
            TcpTransportTests.Run();

            // P5: Simulation
            GameLoopTests.Run();
            DeterministicRandomTests.Run();

            // P6: Session
            LockstepSessionTests.Run();

            // P7: Consistency + Replay
            ConsistencyCheckerTests.Run();
            ReplayTests.Run();

            // Integration: Multi-client determinism
            DeterminismIntegrationTests.Run();

            // Summary (before perf, which doesn't assert)
            var (passed, failed) = TestRunner.Summary();
            Console.WriteLine($"\n=== Correctness: {passed} passed, {failed} failed ===");

            // Perf (separate counters)
            TestRunner.Reset();
            PerfTests.Run();

            return failed > 0 ? 1 : 0;
        }
    }
}
