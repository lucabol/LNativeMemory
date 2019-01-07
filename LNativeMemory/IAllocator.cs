using System;
using System.Collections.Generic;
using System.Text;

namespace LNativeMemory
{
    public interface IAllocator
    {
        ref T Alloc<T>(int sizeOfType = 0, int alignment = 16, in T what = default(T)) where T : unmanaged;
        Span<T> AllocSpan<T>(int n, int sizeOfType = 0, int alignment = 16) where T : unmanaged;

        void Reset();

        long BytesLeft { get; }
        long TotalBytes { get; }
    }
}
