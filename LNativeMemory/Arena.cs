using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LNativeMemory
{

    public unsafe class Arena : IAllocator
    {
        private void* _start;
        private void* _nextAlloc;
        private uint _size;
        private void* _endMemory;

        public Arena(Span<byte> memory)
        {
            _start = _nextAlloc = Unsafe.AsPointer<byte>(ref memory[0]);
            _size = (uint)memory.Length;
            _endMemory = Unsafe.Add<byte>(_start, (int)_size);
            Unsafe.InitBlock(_start, 0, _size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Align(void* mem, int alignment) => (void*)(((ulong)mem + (ulong)alignment - 1) & ~((ulong)alignment - 1));

        public ref T Alloc<T>(int sizeOfType = 0, int alignment = 16, in T what = default(T)) where T : unmanaged
        {
            Debug.Assert(sizeOfType >= 0);
            Debug.Assert(alignment >= 0);
            Debug.Assert(alignment % 2 == 0);

            if (sizeOfType == 0) sizeOfType = sizeof(T);

            _nextAlloc = Align(_nextAlloc, alignment);
            Debug.Assert((ulong)_nextAlloc % (ulong) alignment == 0);

            Debug.Assert((byte*)_nextAlloc + sizeOfType <= _endMemory,
                    $"Trying to allocate {sizeOfType.ToString()} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString()}\nNextAlloc: {((int)_nextAlloc).ToString()}\nSize:{((int)_size).ToString()}");

            var ptr = _nextAlloc;
            _nextAlloc = (byte*)_nextAlloc + sizeOfType;
            ref var ret = ref Unsafe.AsRef<T>(ptr);
            ret = what;
            return ref ret;
        }

        public Span<T> AllocSpan<T>(int n, int sizeOfType = 0, int alignment = 16) where T : unmanaged
        {
            Debug.Assert(sizeOfType >= 0);
            Debug.Assert(alignment >= 0);
            Debug.Assert(alignment % 2 == 0);

            if (sizeOfType == 0) sizeOfType = sizeof(T);
            var sizeOfArray = sizeOfType * n;

            _nextAlloc = Align(_nextAlloc, alignment);
            Debug.Assert((ulong)_nextAlloc % (ulong)alignment == 0);

            Debug.Assert((byte*)_nextAlloc + sizeOfArray <= _endMemory,
                    $"Trying to allocate {sizeOfType.ToString()} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString()}\nNextAlloc: {((int)_nextAlloc).ToString()}\nSize:{((int)_size).ToString()}");

            var ptr = _nextAlloc;
            _nextAlloc = (byte*)_nextAlloc + sizeOfArray;

            Unsafe.InitBlock(ptr, 0, (uint)sizeOfArray);
            return new Span<T>(ptr,n);
        }

        public void Reset()
        {
            Unsafe.InitBlock(_start, 0, _size);
            _nextAlloc = _start;
        }

        public uint BytesLeft => _size - (uint)((byte*)_nextAlloc - (byte*)_start);
        public uint TotalBytes => _size;
    }

    public unsafe sealed class NativeArena : IDisposable
    {

        private IntPtr _start;
        public Arena Arena { get; }

        public NativeArena(int totalBytes)
        {
            Trace.Assert(totalBytes > 0);

            _start = Marshal.AllocHGlobal(totalBytes);
            Arena = new Arena(new Span<byte>(_start.ToPointer(), totalBytes));
        }

        #region IDisposable Support
        ~NativeArena()
        {
            Dispose();
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_start);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}
