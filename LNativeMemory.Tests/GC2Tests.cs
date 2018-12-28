using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LNativeMemory.Tests
{

    // XUnit executes all tests in a class sequentially, so no problem with multithreading calls to GC
    public class GC2Tests
    {

        const int sleepTime = 200;
        // 32 bits workstation GC ephemeral segment size
        // (https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/)
        const int totalBytes = 16 * 1024 * 1024;

        private readonly ITestOutputHelper output;

        public GC2Tests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void NoAllocationToLimit()
        {
            try
            {
                var triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Assert.True(succeeded);
                Thread.Sleep(sleepTime);
                Assert.False(triggered);

                var bytes = new byte[99];
                Thread.Sleep(sleepTime);
                Assert.False(triggered);
            }
            finally
            {
                GC2.EndNoGCRegion();
            }
        }

        [Fact]
        public void AllocatingOverLimitTriggersTheAction()
        {
            try
            {
                var triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(totalBytes, () => triggered = true);
                Assert.True(succeeded);
                Assert.False(triggered);

                for (var i = 0; i < 3; i++) { var k = new byte[totalBytes]; }

                Thread.Sleep(sleepTime);
                Assert.True(triggered);
            }
            finally
            {
                GC2.EndNoGCRegion();
            }
        }

        [Fact]
        public void CanCallMultipleTimes()
        {

            for (int i = 0; i < 3; i++)
            {
                NoAllocationToLimit();
            }
        }
    }
}
