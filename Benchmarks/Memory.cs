using System.Threading;
using System.Diagnostics;
using System;
using System.Runtime.CompilerServices;
using System.Buffers;

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Configs;

using LNativeMemory;

namespace Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Orderer(SummaryOrderPolicy.Declared)]
public unsafe class ArenaBench
{
    [Params(
            100
            , 10_000
            , 1_000_000
            )]
    public int N;

    void* buffer;
    int size;

    ArrayPool<Man> _menPool;
    ArrayPool<Elf> _elvesPool;
    ArrayPool<Dwarf> _dwarvesPool;

    Man[] _menArray;
    Elf[] _elvesArray;
    Dwarf[] _dwarvesArray;

    [GlobalSetup(Targets = new[] {nameof(Arena), nameof(ArenaW), nameof(ArenaR)})]
    public void GlobalSetupArena() {

      size = N * Unsafe.SizeOf<Man>() * Unsafe.SizeOf<Elf>() * Unsafe.SizeOf<Dwarf>();
      buffer = NativeMemory.Alloc((nuint)size);
    }

    [GlobalCleanup(Targets = new[] {nameof(Arena), nameof(ArenaW), nameof(ArenaR)})]
    public void GlobalCleanupArena() {
      NativeMemory.Free(buffer);
    }

    [GlobalSetup(Targets = new[] {nameof(ArrayW), nameof(ArrayR)})]
    public void GlobalSetupArray() {

        _menArray = new Man[N];
        _elvesArray = new Elf[N];
        _dwarvesArray = new Dwarf[N];
    }

    [GlobalSetup(Targets = new[] {nameof(ArrayPool), nameof(ArrayPoolW), nameof(ArrayPoolR)})]
    public void GSDotNetArrayPool() {

      _menPool = ArrayPool<Man>.Shared;
      _elvesPool = ArrayPool<Elf>.Shared;
      _dwarvesPool = ArrayPool<Dwarf>.Shared;

      if(_menPool.Rent(N).Length + _elvesPool.Rent(N).Length + _dwarvesPool.Rent(N).Length < N)
        throw new ArgumentException("Just warming up the pools, but got an exception");

      _menArray = _menPool.Rent(N);
      _elvesArray = _elvesPool.Rent(N);
      _dwarvesArray = _dwarvesPool.Rent(N);
    }

    [BenchmarkCategory("Allocation"), Benchmark(Baseline = true, Description = "Arena")]
    public int Arena() {
      var ar = new Arena<DisableBoundsCheck, NonZeroMemory>(new Span<byte>(buffer, size));

      var men = ar.AllocSpan<Man>(N);
      var elves = ar.AllocSpan<Elf>(N);
      var dwarves = ar.AllocSpan<Dwarf>(N);

      return men.Length + elves.Length + dwarves.Length;
    }

    [BenchmarkCategory("Allocation"), Benchmark(Description = "Array")]
    public int Array() {
      var _menArray = new Man[N];
      var _elvesArray = new Elf[N];
      var _dwarvesArray = new Dwarf[N];

      return _menArray.Length + _elvesArray.Length + _dwarvesArray.Length;
    }
    [BenchmarkCategory("Allocation"), Benchmark(Description = "Pool")]
    public int ArrayPool() {
      var men = _menPool.Rent(N);
      var elves = _elvesPool.Rent(N);
      var dwarves = _dwarvesPool.Rent(N);

      return men.Length + elves.Length + dwarves.Length;
    }

    [BenchmarkCategory("Write"), Benchmark(Baseline = true, Description = "Arena")]
    public int ArenaW() {
      // TODO: there must be a better way.
      var men = new Span<Man>(buffer, N);
      var elves = new Span<Elf>((Man*)buffer + Unsafe.SizeOf<Man>(), N);
      var dwarves = new Span<Dwarf>((Elf*)Unsafe.AsPointer<Elf>(ref elves[0]) + Unsafe.SizeOf<Elf>(), N);

      for(int i = 0; i < N; i++) men[i] = new Man(0, 0, i, i);
      for(int i = 0; i < N; i++) elves[i] = new Elf(0, 0, i, i);
      for(int i = 0; i < N; i++) dwarves[i] = new Dwarf(0, 0, i);

      return men.Length + elves.Length + dwarves.Length;
    }

    [BenchmarkCategory("Write"), Benchmark(Description = "Array")]
    public int ArrayW() {

      for(int i = 0; i < N; i++) _menArray[i] = new Man(0, 0, i, i);
      for(int i = 0; i < N; i++) _elvesArray[i] = new Elf(0, 0, i, i);
      for(int i = 0; i < N; i++) _dwarvesArray[i] = new Dwarf(0, 0, i);

      return _menArray.Length + _elvesArray.Length + _dwarvesArray.Length;
    }

    [BenchmarkCategory("Write"), Benchmark(Description = "Pool")]
    public int ArrayPoolW() {

      for(int i = 0; i < N; i++) _menArray[i] = new Man(0, 0, i, i);
      for(int i = 0; i < N; i++) _elvesArray[i] = new Elf(0, 0, i, i);
      for(int i = 0; i < N; i++) _dwarvesArray[i] = new Dwarf(0, 0, i);

      return _menArray.Length + _elvesArray.Length + _dwarvesArray.Length;
    }

    [BenchmarkCategory("Read"), Benchmark(Baseline = true, Description = "Arena")]
    public int ArenaR() {
      // TODO: there must be a better way.
      var men = new Span<Man>(buffer, N);
      var elves = new Span<Elf>((Man*)buffer + Unsafe.SizeOf<Man>(), N);
      var dwarves = new Span<Dwarf>((Elf*)Unsafe.AsPointer<Elf>(ref elves[0]) + Unsafe.SizeOf<Elf>(), N);

      var sum = 0;
      foreach(var s in men) sum += s.strength * s.stamina;
      foreach(var s in elves) sum += s.age * s.magic * 2;
      foreach(var s in dwarves) sum += s.will * 10;
      return sum;
    }

    [BenchmarkCategory("Read"), Benchmark(Description = "Array")]
    public int ArrayR() {
      var sum = 0;
      foreach(var s in _menArray) sum += s.strength * s.stamina;
      foreach(var s in _elvesArray) sum += s.age * s.magic * 2;
      foreach(var s in _dwarvesArray) sum += s.will * 10;
      return sum;
    }

    [BenchmarkCategory("Read"), Benchmark(Description = "Pool")]
    public int ArrayPoolR() {
      var sum = 0;
      foreach(var s in _menArray) sum += s.strength * s.stamina;
      foreach(var s in _elvesArray) sum += s.age * s.magic * 2;
      foreach(var s in _dwarvesArray) sum += s.will * 10;
      return sum;
    }
  }
