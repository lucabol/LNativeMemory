using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace LNativeMemory.Tests {

    // XUnit executes all tests in a class sequentially, so nothing to do here
    public class GC2Tests {

        [Fact]
        public void NoAllocationToLimit() {
            try {
                var succeeded = GC2.TryStartNoGCRegion(100, () => Assert.True(false, "No GC should have been triggered"));
                Assert.True(succeeded);

                var bytes = new Byte[100];
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

                var bytes = new Byte[1001];
                Thread.Sleep(200);
                Assert.True(triggered);
            } finally {
                GC2.EndNoGCRegion();
            }
        }

        [Fact]
        public void CanCallMultipleTimes() {

            for (int i = 0; i < 3; i++) {
                try {
                    var triggered = false;
                    var succeeded = GC2.TryStartNoGCRegion(100, () => triggered = true);
                    Assert.True(succeeded);

                    var bytes = new Byte[1001];
                    Thread.Sleep(200);
                    Assert.True(triggered);
                } finally {
                    GC2.EndNoGCRegion();
                }
            }
        }
    }
}
