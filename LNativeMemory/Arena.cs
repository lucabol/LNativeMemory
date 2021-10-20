using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LNativeMemory
{
    public struct EnableBoundsCheck { }
    public struct DisableBoundsCheck { }
    public struct ZeroMemory {}
    public struct NonZeroMemory {}

    public unsafe ref struct Arena<TBoundCheckPolicy, TZeroMemoryPolicy>
    {
        private void* _start;
        private void* _nextAlloc;
        private uint _size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Arena(Span<byte> memory)
        {
            _start = _nextAlloc = Unsafe.AsPointer<byte>(ref memory[0]);
            _size = (uint)memory.Length;

            if(typeof(TZeroMemoryPolicy) == typeof(ZeroMemory)) {
              Unsafe.InitBlock(_start, 0, _size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Alloc<T>(int sizeOfType = 0, int alignment = 16) where T : unmanaged
        {
            Debug.Assert(sizeOfType >= 0);
            Debug.Assert(alignment >= 0);
            Debug.Assert(alignment % 2 == 0);

            if (sizeOfType == 0) sizeOfType = sizeof(T);

            _nextAlloc = Align(_nextAlloc, alignment);

            if(typeof(TBoundCheckPolicy) == typeof(EnableBoundsCheck))
            {
                Trace.Assert((ulong)_nextAlloc % (ulong)alignment == 0);
                Trace.Assert((byte*)_nextAlloc + sizeOfType <= Unsafe.Add<byte>(_start, (int)_size),
                        $"Trying to allocate {sizeOfType.ToString(CultureInfo.CurrentCulture)} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString(CultureInfo.CurrentCulture)}\nNextAlloc: {((int)_nextAlloc).ToString(CultureInfo.CurrentCulture)}\nSize:{((int)_size).ToString(CultureInfo.CurrentCulture)}");
            }

            Debug.Assert((ulong)_nextAlloc % (ulong) alignment == 0);
            Debug.Assert((byte*)_nextAlloc + sizeOfType <= Unsafe.Add<byte>(_start, (int)_size),
                    $"Trying to allocate {sizeOfType.ToString(CultureInfo.CurrentCulture)} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString(CultureInfo.CurrentCulture)}\nNextAlloc: {((int)_nextAlloc).ToString(CultureInfo.CurrentCulture)}\nSize:{((int)_size).ToString(CultureInfo.CurrentCulture)}");

            var ptr = _nextAlloc;
            _nextAlloc = (byte*)_nextAlloc + sizeOfType;
            ref var ret = ref Unsafe.AsRef<T>(ptr);

            if(typeof(TZeroMemoryPolicy) == typeof(ZeroMemory)) {
              Unsafe.InitBlock(ptr, 0, (uint)sizeOfType);
            }
            return ref ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AllocSpan<T>(int n, int sizeOfType = 0, int alignment = 16) where T : unmanaged
        {
            Debug.Assert(sizeOfType >= 0);
            Debug.Assert(alignment >= 0);
            Debug.Assert(alignment % 2 == 0);

            if (sizeOfType == 0) sizeOfType = sizeof(T);
            var sizeOfArray = sizeOfType * n;

            _nextAlloc = Align(_nextAlloc, alignment);

            Debug.Assert((ulong)_nextAlloc % (ulong)alignment == 0);
            Debug.Assert((byte*)_nextAlloc + sizeOfArray <= Unsafe.Add<byte>(_start, (int)_size),
                    $"Trying to allocate {sizeOfType.ToString(CultureInfo.CurrentCulture)} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString(CultureInfo.CurrentCulture)}\nNextAlloc: {((int)_nextAlloc).ToString(CultureInfo.CurrentCulture)}\nSize:{((int)_size).ToString(CultureInfo.CurrentCulture)}");

            if(typeof(TBoundCheckPolicy) == typeof(EnableBoundsCheck))
            {
                Trace.Assert((ulong)_nextAlloc % (ulong)alignment == 0);
                Trace.Assert((byte*)_nextAlloc + sizeOfArray <= Unsafe.Add<byte>(_start, (int)_size),
                        $"Trying to allocate {sizeOfArray.ToString(CultureInfo.CurrentCulture)} bytes for a type {typeof(T).FullName}.\nStart: {((int)_start).ToString(CultureInfo.CurrentCulture)}\nNextAlloc: {((int)_nextAlloc).ToString(CultureInfo.CurrentCulture)}\nSize:{((int)_size).ToString(CultureInfo.CurrentCulture)}");
            }

            var ptr = _nextAlloc;
            _nextAlloc = (byte*)_nextAlloc + sizeOfArray;

            return new Span<T>(ptr,n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if(typeof(TZeroMemoryPolicy) == typeof(ZeroMemory)) {
              Unsafe.InitBlock(_start, 0, _size);
            }
            _nextAlloc = _start;
        }

        public long BytesLeft => _size - (long)((byte*)_nextAlloc - (byte*)_start);
        public long TotalBytes => _size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* Align(void* mem, int alignment) => (void*)(((ulong)mem + (ulong)alignment - 1) & ~((ulong)alignment - 1));
    }


    public unsafe ref struct NativeArena<TBoundCheckPolicy, TZeroMemoryPolicy>
    {

        static private IntPtr _start;
        public Arena<TBoundCheckPolicy, TZeroMemoryPolicy> Arena { get; }

        public NativeArena(int totalBytes)
        {
            Trace.Assert(totalBytes > 0);

            _start = Marshal.AllocHGlobal(totalBytes);
            Arena = new Arena<TBoundCheckPolicy, TZeroMemoryPolicy> (new Span<byte>(_start.ToPointer(), totalBytes));
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_start);
        }
    }

}
