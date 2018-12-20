using System;
using Xunit;
using LNativeMemory;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

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

        private const int bufferSize = 10_000;

        public static IEnumerable<object[]> GetAllocator(int numTests) {

            var allData = new List<object[]> {
                new object[] { new NativeArena(bufferSize).Arena }
            };

            return allData.Take(numTests);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanAllocateStruct<T>(T ar) where T : IAllocator {
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

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanAllocatePrimitiveTypes<T>(T ar) where T : IAllocator {
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

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanInitializeAndAllocate<T>(T ar) where T : IAllocator {
            ref var s = ref ar.Alloc(new CStruct { X = 6 });
            Assert.Equal(6, s.X);

            var span = ar.Alloc(10, new CStruct { X = 7 });
            for (int i = 0; i < span.Length; i++) Assert.Equal(7, span[i].X);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void ThrowsExceptionWhenMemoryIsFull<T>(T ar) where T : IAllocator {
            var i = 0;
            Assert.Throws<OutOfMemoryException>(() => {
                while (i < bufferSize + 1) { ar.Alloc<byte>(); i++; }
                return 0;
            });
            Assert.Equal(bufferSize, i);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void GeCorrectRemainingSize<T>(T ar) where T : IAllocator {
                ar.Alloc<float>();
                Assert.Equal((uint)(bufferSize - sizeof(float)), ar.BytesLeft);
                ar.Alloc<decimal>(10);
                Assert.Equal((uint)(bufferSize - sizeof(float) - sizeof(decimal) * 10), ar.BytesLeft);
        }

        [Fact]
        public unsafe void CanUseStackMemory() {
            var buffer = stackalloc byte[100];

            var ar = new Arena(new Span<byte>(&buffer[0], 100));
            var k = ar.Alloc<double>(2);
            Assert.Equal(0, k[0]);
            k[0] = 3;
            Assert.Equal(2, k.Length);

            ar = new Arena(new Span<byte>(&buffer[0], 100));
            k = ar.Alloc<double>(2);
            Assert.Equal(0, k[0]);
            Assert.Equal(2, k.Length);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanFreeJustLastAllocated<T>(T ar) where T : IAllocator {
            ref var c = ref ar.Alloc<CStruct>();
            ref var d = ref ar.Alloc<CStruct>();

            var initialBytes = ar.BytesLeft;
            ar.Free(ref c);
            var currentBytes = ar.BytesLeft;
            Assert.Equal(initialBytes, currentBytes); // Nothing is freed
            ar.Free(ref d);
            currentBytes = ar.BytesLeft;
            Assert.True(currentBytes > initialBytes); // Here we freed some memory
            ar.Free(ref c);
            Assert.True(ar.BytesLeft > currentBytes); // Freed again, aka inverse order works


            var sp1 = ar.Alloc<CStruct>(10);
            var sp2 = ar.Alloc<CStruct>(10);
            initialBytes = ar.BytesLeft;
            ar.Free(sp1);
            Assert.Equal(initialBytes, ar.BytesLeft); // Nothing is freed
            ar.Free(sp2);
            Assert.True(ar.BytesLeft > initialBytes); // Here we freed some memory

        }
    }
}