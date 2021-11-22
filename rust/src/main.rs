use std::env;

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

fn value_army(men: &mut Vec<Man>, elves: &mut Vec<Elf>, dwarves: &mut Vec<Dwarf>, n: usize) -> u32 {

    men.resize(n, Man { strength: 1, stamina: 1 });
    elves.resize(n, Elf { age: 1, magic: 1 });
    dwarves.resize(n, Dwarf { will: 1 });

    let mut sum: u32 = 0;
    for i in 0..n {
        sum += if n % 3 == 0 { men[i].strength * men[i].stamina }
               else if n % 3 == 1 { elves[i].age * elves[i].magic * 2 }
               else { dwarves[i].will * 10 }
    }

    men.clear();
    elves.clear();
    dwarves.clear();
    return sum;
}

fn main() {
    let args: Vec<String> = env::args().collect();
    if args.len() != 3 {
        println!("Must have two args");
        std::process::exit(-1);
    }
    let n = args[1].parse::<usize>().unwrap();
    let m = args[2].parse::<usize>().unwrap();

    let mut men = Vec::with_capacity(n);
    let mut elves = Vec::with_capacity(n);
    let mut dwarves = Vec::with_capacity(n);

    let mut sum:u32 = 0;
    for _i in 0..m {
        sum += value_army(&mut men, &mut elves, &mut dwarves, n);
    }
    std::process::exit(sum as i32);
}
