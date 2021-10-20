using System.Threading.Tasks;
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

/*
// Of course these are faster
[StructLayout(LayoutKind.Auto)]
readonly record struct Man(int strength, int stamina);
[StructLayout(LayoutKind.Auto)]
readonly record struct Elf(int age, int magic);
[StructLayout(LayoutKind.Auto)]
readonly record struct Dwarf(int will);
*/
record struct Man(ulong objectHeader, ulong methodTable, int strength, int stamina);
record struct Elf(ulong objectHeader, ulong methodTable, int age, int magic);
record struct Dwarf(ulong objectHeader, ulong methodTable, int will);

sealed record CMan(int strength, int stamina);
sealed record CElf(int age, int magic);
sealed record CDwarf(int will);

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Orderer(SummaryOrderPolicy.Declared)]
public unsafe class Scenarios
{
    [Params(
            //10,
            100,
            1000
            , 10_000
            , 1_000_000
            //, 10_000_000
            )]
    public int Requests;

    public int N = 100;

    void* buffer;
    int size;

    [GlobalSetup(Targets = new[] {nameof(ArenaServer)})]
    public void GlobalSetupArena() {

      size = N * Unsafe.SizeOf<Man>() * Unsafe.SizeOf<Elf>() * Unsafe.SizeOf<Dwarf>();
      buffer = NativeMemory.Alloc((nuint)size);

      if(N < 1_000) {
        var av = ArenaValueArmy();
        var ar = ArrayValueArmy();
        var po = PoolValueArmy();

        Trace.Assert(av == ar);
        Trace.Assert(av == po);
      }
    }


    int ValueArmy(Span<Man> men, Span<Elf> elves, Span<Dwarf> dwarves) {

      for(int i = 0; i < N; i++) men[i] = new Man(0, 0, i, i);
      for(int i = 0; i < N; i++) elves[i] = new Elf(0, 0, i, i);
      for(int i = 0; i < N; i++) dwarves[i] = new Dwarf(0, 0, i);

      var sum = 0;
      for(int i = 0; i < N; i++) {
        var m = i % 3;
        sum += m == 0 ? men[i].strength * men[i].stamina :
               m == 1 ? elves[i].age * elves[i].magic * 2 :
                        dwarves[i].will * 10;
      }
      return sum;
    }

    int ClassValueArmy(Span<CMan> men, Span<CElf> elves, Span<CDwarf> dwarves) {

      for(int i = 0; i < N; i++) men[i] = new CMan(i, i);
      for(int i = 0; i < N; i++) elves[i] = new CElf(i, i);
      for(int i = 0; i < N; i++) dwarves[i] = new CDwarf(i);

      var sum = 0;
      for(int i = 0; i < N; i++) {
        var m = i % 3;
        sum += m == 0 ? men[i].strength * men[i].stamina :
               m == 1 ? elves[i].age * elves[i].magic * 2 :
                        dwarves[i].will * 10;
      }
      return sum;
    }

    int ArenaValueArmy() {
      var ar = new Arena<DisableBoundsCheck, NonZeroMemory>(new Span<byte>(buffer, size));

      var men = ar.AllocSpan<Man>(N);
      var elves = ar.AllocSpan<Elf>(N);
      var dwarves = ar.AllocSpan<Dwarf>(N);

      var r = ValueArmy(men, elves, dwarves);
      ar.Reset();
      return r;
    }

    int ArrayValueArmy() {
      var men = new Man[N];
      var elves = new Elf[N];
      var dwarves = new Dwarf[N];

      var r = ValueArmy(men, elves, dwarves);
      return r;
    }

    int PoolValueArmy() {
      var men = ArrayPool<Man>.Shared.Rent(N);
      var elves = ArrayPool<Elf>.Shared.Rent(N);
      var dwarves = ArrayPool<Dwarf>.Shared.Rent(N);

      var r = ValueArmy(men, elves, dwarves);

      ArrayPool<Man>.Shared.Return(men);
      ArrayPool<Elf>.Shared.Return(elves);
      ArrayPool<Dwarf>.Shared.Return(dwarves);
      return r;
    }

    int ClassArrayValueArmy() {
      var men = new CMan[N];
      var elves = new CElf[N];
      var dwarves = new CDwarf[N];

      var r = ClassValueArmy(men, elves, dwarves);
      return r;
    }

    int ProcessRequests(Func<int> f) {
      var req = new Task<int>[Requests];
      for(int i =0; i < Requests; i++) {
        req[i] = Task.Run<int>(f);
      }
      Task.WaitAll(req);

      var sum = 0;
      foreach(var t in req) sum += t.Result;
      return sum;
    }

    [BenchmarkCategory("Server"), Benchmark(Baseline = true, Description = "Pool")]
    public int ArrayPoolServer() => ProcessRequests(PoolValueArmy);

    [BenchmarkCategory("Server"), Benchmark(Description = "Arena")]
    public int ArenaServer() => ProcessRequests(ArenaValueArmy);

    [BenchmarkCategory("Server"), Benchmark(Description = "Array")]
    public int ArrayServer() => ProcessRequests(ArrayValueArmy);

    [BenchmarkCategory("Server"), Benchmark(Description = "Class")]
    public int ClassArrayServer() => ProcessRequests(ClassArrayValueArmy);
  }
