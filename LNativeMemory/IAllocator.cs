using System;
using System.Collections.Generic;
using System.Text;

namespace LNativeMemory
{
    public interface IAllocator
    {
        ref T Alloc<T>() where T : unmanaged;
        ref T Alloc<T>(in T toCopy) where T : unmanaged;
        ref T FastAlloc<T>(int sizeType) where T : unmanaged;

        Span<T> Alloc<T>(int n, in T toCopy) where T : unmanaged;
        Span<T> Alloc<T>(int n) where T : unmanaged;
        Span<T> FastAlloc<T>(int n, int sizeArrayInBytes) where T : unmanaged;

        void Reset();

        uint BytesLeft { get; }
        uint TotalBytes { get; }
    }
}
