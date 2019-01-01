using System;
using Xunit;


namespace LNativeMemory.Tests
{
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

    public class PackTest
    {
        [Fact]
        public unsafe void TestPackingOfStruct()
        {
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
            Assert.Equal(16, sizeof(decimal));

            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCFBPL));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCIPS));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SCPS));
            Assert.Equal(16, sizeof(LNativeMemory.Tests.SPC));
            Assert.Equal(24, sizeof(LNativeMemory.Tests.SPCL));
            Assert.Equal(16, sizeof(LNativeMemory.Tests.SPSC));
            Assert.Equal(4, sizeof(LNativeMemory.Tests.SSC));
        }
    }
}
