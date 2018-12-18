using System;
using Xunit;
using LNativeMemory;
using System.Runtime.InteropServices;

namespace LNativeMemory.Tests {
    [StructLayout(LayoutKind.Auto)]
    struct CStruct {
        public int X;
        public float Y;
        bool b;
        double d;
        Decimal dec;
    }
    public class Tests {

        [Fact]
        public void CanAllocateStruct() {
            using (var ar = new Arena(1_000_000)) {
                ref var s = ref ar.Alloc<CStruct>();
                Assert.Equal(0, s.X);
                s.X = 3;
                Assert.Equal(3, s.X);

                var n = 100;
                var span = ar.Alloc<CStruct>(n);
                foreach (var c in span) Assert.Equal(0, c.X);
                for (int i = 0; i < n; i++) span[i].X = 3;
                foreach (var c in span) Assert.Equal(3, c.X);
            }
        }

        [Fact]
        public void CanAllocatePrimitiveTypes() {
            using (var ar = new Arena(1_000)) {
                var ispan = ar.Alloc<int>(10);
                var fspan = ar.Alloc<float>(10);
                var dspan = ar.Alloc<double>(10);
                var bspan = ar.Alloc<bool>(10);
                var despan = ar.Alloc<Decimal>(10);

                for (int i = 0; i < 10; i++) {
                    Assert.Equal(0, ispan[i]);
                    Assert.Equal(0.0, fspan[i]);
                    Assert.Equal(0.0, dspan[i]);
                    Assert.Equal(0, despan[i]);
                    Assert.False(bspan[i]);

                    ispan[i] = 3;
                    fspan[i] = 3.0f;
                    dspan[i] = 3.0;
                    bspan[i] = true;
                    despan[i] = 3;

                    Assert.Equal(3, ispan[i]);
                    Assert.Equal(3.0, fspan[i]);
                    Assert.Equal(3.0, dspan[i]);
                    Assert.Equal(3, despan[i]);
                    Assert.True(bspan[i]);
                }

            }
        }

        [Fact]
        public void ThrowsExceptionWhenMemoryIsFull() {
            using (var ar = new Arena(100)) {
                var i = 0;
                Assert.Throws<OutOfMemoryException>(() => {
                    while (i < 1_000_000) { ar.Alloc<byte>(); i++; }
                    return 0;
                });
                Assert.Equal(100, i);
            }
        }

        [Fact]
        public void GeCorrectRemainingSize() {
            using (var ar = new Arena(1000)) {
                ar.Alloc<float>();
                Assert.Equal(1000 - sizeof(float), ar.BytesLeft);
                ar.Alloc<decimal>(10);
                Assert.Equal(1000 - sizeof(float) - sizeof(decimal) * 10, ar.BytesLeft);
            }
        }
    }
}