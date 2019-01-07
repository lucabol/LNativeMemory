using System.Threading;
using System.Diagnostics;
using LNativeMemory;
using System;

[assembly: CLSCompliant(false)]

namespace MyConsoleTest
{
    // XUnit executes all tests in a class sequentially, so no problem with multithreading calls to GC
    public static class GC2Tests
    {

        const int sleepTime = 100;
        // 32 bits workstation GC ephemeral segment size
        // (https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/)
        const int totalBytes = 16 * 1024 * 1024;
        static bool triggered = false;


        internal static void NoAllocationBeforeLimit()
        {
            try
            {
                triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Trace.Assert(succeeded);
                Thread.Sleep(sleepTime);
                Trace.Assert(!triggered);

                var bytes = new byte[99];
                Thread.Sleep(sleepTime);
                Trace.Assert(!triggered);

            }
            finally
            {
                GC2.EndNoGCRegion();
                triggered = false;
            }
        }

        internal static void AllocatingOverLimitTriggersTheAction()
        {
            try
            {
                triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Trace.Assert(succeeded);
                Trace.Assert(!triggered);

                for (var i = 0; i < 3; i++) { var k = new byte[totalBytes]; }

                Thread.Sleep(sleepTime);
                Trace.Assert(triggered);
            }
            finally
            {
                GC2.EndNoGCRegion();
                triggered = false;
            }
        }

        internal static void CanCallMultipleTimes()
        {

            for (int i = 0; i < 3; i++)
            {
                NoAllocationBeforeLimit();
            }
        }

        internal static void CanUseNoGCRegion()
        {
            triggered = false;
            using (new NoGCRegion(totalBytes, () => triggered = true))
            {
                for (var i = 0; i < 3; i++) { var k = new byte[totalBytes]; }
                Thread.Sleep(sleepTime);
                Trace.Assert(triggered);
                triggered = false;
            }
        }

        public static void Main()
        {
            GC2Tests.AllocatingOverLimitTriggersTheAction();
            GC2Tests.CanCallMultipleTimes();
            GC2Tests.CanUseNoGCRegion();
            GC2Tests.NoAllocationBeforeLimit();
        }
    }
}