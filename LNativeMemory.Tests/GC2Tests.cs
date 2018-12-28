using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LNativeMemory.Tests {



    // XUnit executes all tests in a class sequentially, so nothing to do here
    public class GC2Tests {

        const int sleepTime = 200;
        private readonly ITestOutputHelper output;

        public GC2Tests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void NoAllocationToLimit() {
            try {
                var triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(10_000, () => {
                    triggered = true;
                });
                Assert.True(succeeded, "Not entered NoGC Region");
                Thread.Sleep(sleepTime);
                Assert.True(triggered, "Here we have not allocated anything");

                var bytes = new Byte[99];
                Thread.Sleep(sleepTime);
                Assert.False(triggered, "No GC should have been triggered");
            } finally {
                GC2.EndNoGCRegion();
            }
        }

        [Fact]
        public void AllocatingOverLimitTriggersTheAction() {
            try {
                var triggered = false;
                var succeeded = GC2.TryStartNoGCRegion(100, () => triggered = true);
                Assert.True(succeeded);
                Assert.False(triggered, "No GC before uber allocation");

                var bytes = new Byte[1001];
                Thread.Sleep(sleepTime);
                Assert.True(triggered, "No Gc Should have been triggered");
            } finally {
                GC2.EndNoGCRegion();
            }
        }

        [Fact]
        public void CanCallMultipleTimes() {

            for (int i = 0; i < 3; i++) {
                NoAllocationToLimit();
            }
        }
    }
}
