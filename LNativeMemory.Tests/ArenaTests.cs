using System;
using Xunit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace LNativeMemory.Tests
{

#pragma warning disable CA1823
    [StructLayout(LayoutKind.Auto)]
    struct CStruct
    {
        public int X;
        public float Y;
        bool b;
        double d;
        Decimal dec;
    }
#pragma warning restore CA1823

    public sealed class ArenaTests : IDisposable
    {

        private const int bufferSize = 10_000;

        public ArenaTests()
        {

        }

        [Fact]
        public void CanAllocateStruct()
        {
            using var na = new NativeArena<EnableBoundsCheck, ZeroMemory>(bufferSize);
            var ar = na.Arena;
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

        [Fact]
        public void CanAllocatePrimitiveTypes()
        {
            using var na = new NativeArena<EnableBoundsCheck, ZeroMemory>(bufferSize);
            var ar = na.Arena;
            var ispan = ar.AllocSpan<int>(10);
            var fspan = ar.AllocSpan<float>(10);
            var dspan = ar.AllocSpan<double>(10);
            var bspan = ar.AllocSpan<bool>(10);
            var despan = ar.AllocSpan<Decimal>(10);

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(0, ispan[i]);
                Assert.Equal(0, fspan[i]);
                Assert.Equal(0, dspan[i]);
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

        [Fact]
        public unsafe void AlignsCorrectly()
        {
            using var na = new NativeArena<EnableBoundsCheck, ZeroMemory>(bufferSize);
            var ar = na.Arena;
            ar.Alloc<byte>(); // tries to screw the alignment
            ref var de = ref ar.Alloc<decimal>();
            Assert.Equal(0, (long)Unsafe.AsPointer(ref de) % 8);
            ar.Alloc<byte>();

            ref var d = ref ar.Alloc<double>();
            Assert.Equal(0, (long)Unsafe.AsPointer(ref de) % 4);

            var cacheLine = ar.AllocSpan<double>(10, alignment: 64);
            Assert.Equal(0, (long)Unsafe.AsPointer(ref cacheLine[0]) % 64);
        }


        [Fact]
        public void CanInitializeAndAllocate()
        {
            using var na = new NativeArena<EnableBoundsCheck, ZeroMemory>(bufferSize);
            var ar = na.Arena;
            ref var s = ref ar.Alloc<CStruct>();
            s = new CStruct { X = 6 };
            Assert.Equal(6, s.X);

            var span = ar.AllocSpan<CStruct>(10);
            for (int i = 0; i < span.Length; i++) Assert.Equal(0, span[i].X);
            for (int i = 0; i < span.Length; i++) span[i].X = 7;
            for (int i = 0; i < span.Length; i++) Assert.Equal(7, span[i].X);
        }

        [Fact]
        public void GeApproxForAlignmentCorrectRemainingSize()
        {
            using var na = new NativeArena<EnableBoundsCheck, NonZeroMemory>(bufferSize);
            var ar = na.Arena;
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

            var ar = new Arena<EnableBoundsCheck, ZeroMemory>(new Span<byte>(&buffer[0], 100));
            var k = ar.AllocSpan<double>(2);
            Assert.Equal(0, k[0]);
            k[0] = 3;
            Assert.Equal(2, k.Length);

            var ar2 = new Arena<DisableBoundsCheck, NonZeroMemory>(new Span<byte>(&buffer[0], 100));
            k = ar2.AllocSpan<double>(2);
            k[0] = 0;
            Assert.Equal(0, k[0]);
            Assert.Equal(2, k.Length);
        }

        [Fact]
        public void ResetWorks()
        {
            using var na = new NativeArena<EnableBoundsCheck, ZeroMemory>(bufferSize);
            var ar = na.Arena;
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
