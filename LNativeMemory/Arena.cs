using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LNativeMemory {

    internal unsafe static class Utils {
        internal static void ZeroMem(byte* start, int size) { for (int i = 0; i < size; ++i) start[i] = 0;}
    }

    // It is a mutable value type implementing an interface, all hell break loose!
    // But C# doesn't box it when in an using statement, otherwise it is supposed to be passed around by ref
    public unsafe struct Arena : IDisposable {
        private byte* _start;
        private byte* _nextAlloc;
        private int _size;
        private bool _ownsMemory;

        public Arena(int totalBytes) {
            Debug.Assert(totalBytes > 0);

            _ownsMemory = true;
            _start = _nextAlloc = (byte*)Marshal.AllocHGlobal(totalBytes).ToPointer();
            _size = totalBytes;
            Utils.ZeroMem(_start, _size);
        }

        public Arena(byte* start, int totalBytes) {
            Debug.Assert(totalBytes > 0);

            _ownsMemory = false;
            _start = _nextAlloc = start;
            _size = totalBytes;
            Utils.ZeroMem(_start, _size);
        }

        public ref T Alloc<T>() where T : unmanaged {
            var sizeType = sizeof(T);
            if (_nextAlloc + sizeType > _start + _size) throw new OutOfMemoryException
                    ($"Trying to allocate {sizeType} bytes for a type {typeof(T).FullName}.\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{(int)_size}");

            return ref FastAlloc<T>(sizeType);
        }

        public ref T Alloc<T>(in T toCopy) where T : unmanaged {
            ref var r = ref Alloc<T>();
            r = toCopy;
            return ref r;
        }

        public Span<T> Alloc<T>(int n, in T toCopy) where T : unmanaged {
            var span = Alloc<T>(n);
            for (int i = 0; i < n; i++) span[i] = toCopy;           
            return span;
        }


        public Span<T> Alloc<T>(int n) where T : unmanaged {
            var sizeArray = sizeof(T) * n;
            if (_nextAlloc + sizeArray > _start + _size) throw new OutOfMemoryException
                    ($"Trying to allocate {sizeArray} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            return FastAlloc<T>(n, sizeArray);
        }

        public ref T FastAlloc<T>(int sizeType) where T : unmanaged {
            Debug.Assert(_nextAlloc + sizeType <= _start + _size,
                    $"Trying to allocate {sizeType} bytes for a type {typeof(T).FullName}.\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{(int)_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeType;
            return ref (*((T*)ptr));
        }

        public Span<T> FastAlloc<T>(int n, int sizeArrayInBytes) where T : unmanaged {
            Debug.Assert(_nextAlloc + sizeArrayInBytes <= _start + _size,
                    $"Trying to allocate {sizeArrayInBytes} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeArrayInBytes;
            return new Span<T>(ptr, n);
        }

        public int BytesLeft => _size - (int)(_nextAlloc - _start);
        public int TotalBytes => _size;

        public void Dispose() {
            if (_ownsMemory) Marshal.FreeHGlobal((IntPtr)_start);
        }
    }

}
