using Benchmarks;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

public static class Program {
  //static void Main(string[] args) => BenchmarkRunner.Run<ArenaBench>(args:args);
  //static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
  static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
