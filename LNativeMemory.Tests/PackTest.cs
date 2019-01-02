using System;
using System.Runtime.InteropServices;
using Xunit;


namespace LNativeMemory.Tests
{
#pragma warning disable 0169 // Unused variables in struct
    unsafe struct SPCL
    {
        byte* p;
        char c;
        long x;
    };

    unsafe struct SCFBPL
    {
        byte c;      /* 1 byte */
        fixed byte pad[7]; /* 7 bytes */
        char* p;     /* 8 bytes */
        long x;      /* 8 bytes */
    };

    unsafe struct SPC
    {
        void* p;     /* 8 bytes */
        char c;      /* 1 byte */
    };

    struct SSC
    {
        short s;     /* 2 bytes */
        char c;      /* 1 byte */
    };

    unsafe struct SCIPS
    {
        char c;
        struct foo5_inner
        {
            char* p;
            short x;
        }
        foo5_inner inner;
    };

    unsafe struct SCPS
    {
        char c;
        SCPS* p;
        short x;
    };

    unsafe struct SPSC
    {
        SPSC* p;
        short x;
        char c;
    };

    struct SUPSC
    {
        unsafe struct foo12_inner
        {
            char* p;
            short x;
        }
        foo12_inner inner;
        char c;
    };
#pragma warning restore 0169

    public class PackTest
    {
        [Fact]
        public unsafe void TestPackingOfStruct()
        {
            // Size of all types that satisfies the 'unmanaged' constraint
            Assert.Equal(1, sizeof(byte));
            Assert.Equal(2, sizeof(char));
            Assert.Equal(2, sizeof(short));
            Assert.Equal(4, sizeof(int));
            Assert.Equal(4, sizeof(float));
            Assert.Equal(8, sizeof(double));
            Assert.Equal(8, sizeof(long));
            Assert.Equal(8, sizeof(IntPtr));
            Assert.Equal(8, sizeof(char*));
            Assert.Equal(8, sizeof(void*));
            Assert.Equal(8, sizeof(IntPtr*));
            Assert.Equal(16, sizeof(decimal));

            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCFBPL));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCIPS));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCPS));
            Assert.Equal(16, sizeof(LNativeMemory.Tests.SPC));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SPCL));
            Assert.Equal(16, sizeof(LNativeMemory.Tests.SPSC));
            Assert.Equal(4, sizeof(LNativeMemory.Tests.SSC));


            Assert.Equal(24, Marshal.SizeOf<LNativeMemory.Tests.SCFBPL>());
            Assert.Equal(24, Marshal.SizeOf<LNativeMemory.Tests.SCIPS>());
            Assert.Equal(24, Marshal.SizeOf<LNativeMemory.Tests.SCPS>());
            Assert.Equal(16, Marshal.SizeOf<LNativeMemory.Tests.SPC>());
            Assert.Equal(24, Marshal.SizeOf<LNativeMemory.Tests.SPCL>());
            Assert.Equal(16, Marshal.SizeOf<LNativeMemory.Tests.SPSC>());
            Assert.Equal(4, Marshal.SizeOf<LNativeMemory.Tests.SSC>());

        }

        [Fact]
        unsafe static void MallocIs16Aligned()
        {
            var r = new Random();
            for (int i = 0; i < 100; i++)
            {
                var b = Marshal.AllocHGlobal(r.Next(100, 500));
                Assert.Equal(0, b.ToInt64() % 16);
            }
        }

        [Fact]
        unsafe static void AligningAlgosWork()
        {
            var alignments = new int[] { 1, 2, 4, 8, 16, 32, 64 };
            var r = new Random();

            var aFuncs = new Func<long, long, long>[]
            {
                (m,a) => (m + a - 1) / a * a,   // Works for all alignment values
                (m,a) => (m + a - 1) & ~ (a - 1) // Works for power of 2 alignment values, likely quicker
            };

            foreach (var a in alignments)
            {
                for (int i = 0; i < a - 1; i++)
                {
                    var m = Marshal.AllocHGlobal(r.Next(100, 500)).ToInt64();
                    m += i; // Misalign it by i values
                    foreach (var f in aFuncs)
                    {
                        var am = f(m, a);
                        Assert.True(am >= m);    // It is after the aligned memory
                        Assert.Equal(0, am % a); // It is aligned to a
                    }
                }
            }

        }

    }
}
