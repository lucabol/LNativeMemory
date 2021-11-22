using System;
using System.Runtime.InteropServices;

static int ValueArmy(Man[] men, Elf[] elves, Dwarf[] dwarves, int N) {

  Array.Fill(men, new Man(1,1));
  Array.Fill(elves, new Elf(1,1));
  Array.Fill(dwarves, new Dwarf(1));
  var sum = 0;

  for(int i = 0; i < N; i++) {
    var m = i % 3;
    sum += m == 0 ? men[i].strength * men[i].stamina :
           m == 1 ? elves[i].age * elves[i].magic * 2 :
                    dwarves[i].will * 10;
  }

  return sum;
}

GC.TryStartNoGCRegion(200_000_000);

if(args.Length != 2) {
  Console.WriteLine("Only two params");
  return -1;
}
var N = Convert.ToInt32(args[0]);
var M = Convert.ToInt32(args[1]);

var men = new Man[N];
var elves = new Elf[N];
var dwarves = new Dwarf[N];


var sum = 0;
for(var i = 0; i < M; i++) {
  sum += ValueArmy(men, elves, dwarves, N);
}

return sum;

[StructLayout(LayoutKind.Auto)]
readonly record struct Man(int strength, int stamina);
[StructLayout(LayoutKind.Auto)]
readonly record struct Elf(int age, int magic);
[StructLayout(LayoutKind.Auto)]
readonly record struct Dwarf(int will);

// Fix bug with my version of OmniSharp
namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }
