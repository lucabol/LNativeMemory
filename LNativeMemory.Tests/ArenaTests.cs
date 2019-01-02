using System;
using Xunit;
using LNativeMemory;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LNativeMemory.Tests
{

    [StructLayout(LayoutKind.Auto)]
    struct CStruct
    {
        public int X;
        public float Y;
        bool b;
        double d;
        Decimal dec;
    }

    public class Tests : IDisposable
    {

        private const int bufferSize = 10_000;

        public Tests()
        {

        }

        public static IEnumerable<object[]> GetAllocator(int numTests)
        {

            var allData = new List<object[]> {
                new object[] { new NativeArena(bufferSize).Arena }
            };

            return allData.Take(numTests);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanAllocateStruct<T>(T ar) where T : IAllocator
        {
            ref var s = ref ar.Alloc<CStruct>();
            Assert.Equal(0, s.X);
            s.X = 3;
            Assert.Equal(3, s.X);

            var n = 100;
            var span = ar.AllocSpan<CStruct>(n);
            foreach (var c in span) Assert.Equal(0, c.X);
            for (int i = 0; i < n; i++) span[i].X = 3;
            foreach (var c in span) Assert.Equal(3, c.X);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanAllocatePrimitiveTypes<T>(T ar) where T : IAllocator
        {
            var ispan = ar.AllocSpan<int>(10);
            var fspan = ar.AllocSpan<float>(10);
            var dspan = ar.AllocSpan<double>(10);
            var bspan = ar.AllocSpan<bool>(10);
            var despan = ar.AllocSpan<Decimal>(10);

            for (int i = 0; i < 10; i++)
            {
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
        public unsafe void AlignsCorrectly<T>(T ar) where T : IAllocator
        {
            ar.Alloc<byte>(); // tries to screw the alignment
            ref var de = ref ar.Alloc<decimal>();
            Assert.Equal(0, (long)Unsafe.AsPointer(ref de) % 8);
            ar.Alloc<byte>();

            ref var d = ref ar.Alloc<double>();
            Assert.Equal(0, (long)Unsafe.AsPointer(ref de) % 4);

            var cacheLine = ar.AllocSpan<double>(10, alignment: 64);
            Assert.Equal(0, (long)Unsafe.AsPointer(ref cacheLine[0]) % 64);
        }


        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void CanInitializeAndAllocate<T>(T ar) where T : IAllocator
        {
            ref var s = ref ar.Alloc(what: new CStruct { X = 6 });
            Assert.Equal(6, s.X);

            var span = ar.AllocSpan<CStruct>(10);
            for (int i = 0; i < span.Length; i++) Assert.Equal(0, span[i].X);
            for (int i = 0; i < span.Length; i++) span[i].X = 7;
            for (int i = 0; i < span.Length; i++) Assert.Equal(7, span[i].X);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void GeApproxForAlignmentCorrectRemainingSize<T>(T ar) where T : IAllocator
        {
            // Variable alignment requires these tests not to be simple equalities, approx so that remaining bytes are in a reasonable range
            ar.Alloc<float>();
            Assert.True(ar.BytesLeft <= (uint)(bufferSize - sizeof(float)));
            Assert.True(ar.BytesLeft > (uint)(bufferSize - sizeof(float) * 2));

            ar.AllocSpan<decimal>(10);
            Assert.True(ar.BytesLeft <= (uint)(bufferSize - sizeof(float) - sizeof(decimal) * 10));
            Assert.True(ar.BytesLeft > (uint)(bufferSize - sizeof(float) * 2 - sizeof(decimal) * 10 * 2));
        }

        [Fact]
        public unsafe void CanUseStackMemory()
        {
            var buffer = stackalloc byte[100];

            var ar = new Arena(new Span<byte>(&buffer[0], 100));
            var k = ar.AllocSpan<double>(2);
            Assert.Equal(0, k[0]);
            k[0] = 3;
            Assert.Equal(2, k.Length);

            ar = new Arena(new Span<byte>(&buffer[0], 100));
            k = ar.AllocSpan<double>(2);
            Assert.Equal(0, k[0]);
            Assert.Equal(2, k.Length);
        }

        [Theory]
        [MemberData(nameof(GetAllocator), parameters: 1)]
        public void ResetWorks<T>(T ar) where T : IAllocator
        {
            ar.AllocSpan<CStruct>(20);
            Assert.True(ar.BytesLeft < ar.TotalBytes);
            ar.Reset();

            Assert.Equal(bufferSize, (int)ar.BytesLeft);
            var span = ar.AllocSpan<CStruct>(20);
            Assert.Equal(0, span[0].X);
        }

        public void Dispose()
        {

        }
    }
}