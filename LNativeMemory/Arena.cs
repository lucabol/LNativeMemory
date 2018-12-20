using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LNativeMemory {

    public unsafe class Arena: IAllocator {
        protected void* _start;
        private void* _nextAlloc;
        private uint _size;
        private void* _endMemory;

        public Arena(Span<byte> memory) {
            _start = _nextAlloc = Unsafe.AsPointer<byte>(ref memory[0]);
            _size = (uint) memory.Length;
            _endMemory = Unsafe.Add<byte>(_start, (int)_size);
            Unsafe.InitBlock(_start, 0, _size);
        }

        public ref T Alloc<T>() where T : unmanaged {
            var sizeType = Unsafe.SizeOf<T>();
            var endAlloc = Unsafe.Add<T>(_nextAlloc, 1);

            if (endAlloc > _endMemory) throw new OutOfMemoryException
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
            var sizeArray = Unsafe.SizeOf<T>() * n;
            var endAlloc = Unsafe.Add<T>(_nextAlloc, n);
            if (endAlloc > _endMemory) throw new OutOfMemoryException
                    ($"Trying to allocate {sizeArray} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            return FastAlloc<T>(n, sizeArray);
        }

        public ref T FastAlloc<T>(int sizeType) where T : unmanaged {
            var endAlloc = Unsafe.Add<T>(_nextAlloc, 1);
            Debug.Assert(endAlloc <= _endMemory,
                    $"Trying to allocate {sizeType} bytes for a type {typeof(T).FullName}.\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{(int)_size}");

            var ptr = _nextAlloc;
            _nextAlloc = (byte*)_nextAlloc + sizeType;
            return ref Unsafe.AsRef<T>(ptr);
        }

        public Span<T> FastAlloc<T>(int n, int sizeArrayInBytes) where T : unmanaged {
            Debug.Assert((byte*) _nextAlloc + sizeArrayInBytes <= _endMemory,
                    $"Trying to allocate {sizeArrayInBytes} bytes for an array {typeof(T).FullName}[{n}].\nStart: {(int)_start}\nNextAlloc: {(int)_nextAlloc}\nSize:{_size}");

            var ptr = _nextAlloc;
            _nextAlloc = (byte*) _nextAlloc + sizeArrayInBytes;
            return new Span<T>(ptr, n);
        }

        public void Free<T>(ref T item) where T: unmanaged {
            FastFree(ref item, Unsafe.SizeOf<T>());
        }

        public unsafe void FastFree<T>(ref T item, int size) where T : unmanaged {
            byte* addr = (byte*) Unsafe.AsPointer(ref item);
            if (addr + size == _nextAlloc) _nextAlloc = addr;
        }

        public void Free<T>(Span<T> span) where T : unmanaged {
            FastFree(span, sizeof(T));
        }

        public unsafe void FastFree<T>(Span<T> span, int size) where T : unmanaged {
            byte* addr = (byte*)Unsafe.AsPointer(ref span[0]);
            if (addr + size * span.Length == _nextAlloc) _nextAlloc = addr;
        }

        public uint BytesLeft => _size - (uint)((byte*)_nextAlloc - (byte*)_start);
        public uint TotalBytes => _size;
    }

    public unsafe class NativeArena: IDisposable {

        private IntPtr _start;
        public Arena Arena { get; }

        public NativeArena(int totalBytes) {
            Trace.Assert(totalBytes > 0);

            _start = Marshal.AllocHGlobal(totalBytes);
            Arena = new Arena(new Span<byte>(_start.ToPointer(), totalBytes));
        }

        #region IDisposable Support
        ~NativeArena() {
            Dispose();
        }

        public void Dispose() {
            Marshal.FreeHGlobal(_start);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}
