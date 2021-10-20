#[derive(Clone)]
struct Man {
    strength: u32,
    stamina: u32,
}
#[derive(Clone)]
struct Elf {
    age: u32,
    magic: u32,
}
#[derive(Clone)]
struct Dwarf {
    will: u32,
}

struct WarSystem {
    men: Vec<Man>,
    elves: Vec<Elf>,
    dwarves: Vec<Dwarf>,
}

impl WarSystem {
    pub fn init(soldiers: usize) -> WarSystem {
        return WarSystem {
            men: vec![
                Man {
                    strength: 1,
                    stamina: 1
                };
                soldiers / 3
            ],
            elves: vec![Elf { age: 1, magic: 1 }; soldiers / 3],
            dwarves: vec![Dwarf { will: 1 }; soldiers / 3],
        };
    }

    pub fn value_army(self: &Self) -> u32 {
        let mut sum: u32 = 0;
        for s in &self.men {
            sum += s.strength * s.stamina;
        }
        for s in &self.elves {
            sum += s.age * s.magic * 2;
        }
        for s in &self.dwarves {
            sum += s.will * 10;
        }
        return sum;
    }
}

pub fn main() {
    let w = WarSystem::init(30_000_000);

    let v = w.value_army();
    if v != 130_000_000 {
        panic!("Wrong Sum");
    };
}
