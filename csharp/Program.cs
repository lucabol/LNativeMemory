readonly record struct Man(int strength, int stamina);
readonly record struct Elf(int age, int magic);
readonly record struct Dwarf(int will);

sealed class WarSystem {
  Man[] men;
  Elf[] elves;
  Dwarf[] dwarves;

  public WarSystem(int soldiers) {
    men = new Man[soldiers / 3];
    elves = new Elf[soldiers / 3];
    dwarves = new Dwarf[soldiers / 3];

    Array.Fill(men, new Man(1,1));
    Array.Fill(elves, new Elf(1,1));
    Array.Fill(dwarves, new Dwarf(1));
  }

  public int ValueArmy() {

    var sum = 0;
    foreach(var s in men) sum += s.strength * s.stamina;
    foreach(var s in elves) sum += s.age * s.magic * 2;
    foreach(var s in dwarves) sum += s.will * 10;
    return sum;
  }
  
  static int Main() {
    WarSystem w = new(30_000_000);
    var v = w.ValueArmy();
    if (v == 130_000_000) return 0; else throw new Exception("Wrong sum");
  }
}
