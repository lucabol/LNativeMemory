using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LNativeMemory {

    // It is a mutable value type implementing an interface, all hell break loose!
    // But C# doesn't box it when in an using statement, otherwise it is supposed to be passed around by ref
    public unsafe struct Arena : IDisposable {
        private byte* _start;
        private byte* _nextAlloc;
        private int _size;

        public Arena(int totalBytes) {
            Debug.Assert(totalBytes > 0);

            _start = _nextAlloc = (byte*)Marshal.AllocHGlobal(totalBytes).ToPointer();
            _size = totalBytes;
            for (int i = 0; i < totalBytes; ++i) _start[i] = 0;
        }

        public ref T Alloc<T>() where T : unmanaged {
            var sizeType = sizeof(T);
            if (_nextAlloc + sizeType > _start + _size) throw new OutOfMemoryException
                    ($"Trying to allocate {sizeType} bytes for a type {typeof(T).FullName}.\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{(int)_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeType;
            return ref (*((T*)ptr));
        }

        public Span<T> Alloc<T>(int n) where T : unmanaged {
            var sizeArray = sizeof(T) * n;
            if (_nextAlloc + sizeArray > _start + _size) throw new OutOfMemoryException
                    ($"Trying to allocate {sizeArray} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeArray;
            return new Span<T>(ptr, n);
        }

        public ref T FastAlloc<T>() where T : unmanaged {
            var sizeType = sizeof(T);
            Debug.Assert(_nextAlloc + sizeType <= _start + _size,
                    $"Trying to allocate {sizeType} bytes for a type {typeof(T).FullName}.\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{(int)_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeType;
            return ref (*((T*)ptr));
        }

        public Span<T> FastAlloc<T>(int n) where T : unmanaged {
            var sizeArray = sizeof(T) * n;
            Debug.Assert(_nextAlloc + sizeArray <= _start + _size,
                    $"Trying to allocate {sizeArray} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            var ptr = _nextAlloc;
            _nextAlloc += sizeArray;
            return new Span<T>(ptr, n);
        }

        public int BytesLeft => _size - (int)(_nextAlloc - _start);
        public int TotalBytes => _size;

        public void Dispose() {
            Marshal.FreeHGlobal((IntPtr)_start);
        }
    }

}
