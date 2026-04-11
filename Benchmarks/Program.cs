using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarks.Actions;
using Benchmarks.Models;
using static Benchmarks.Helpers.ChartGenerator;

string outputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkResults");

CustomConfig testConfig = new (outputPath);

/*
Summary ed1 = BenchmarkRunner.Run<SmallNBenchmarksEd25519>(testConfig);
GenerateChart(ed1, outputPath,Path.Combine("img", "Ed25519_Performance_small"));


Summary ed2 = BenchmarkRunner.Run<LargeNBenchmarksEd25519>(testConfig);
GenerateChart(ed2, outputPath,Path.Combine("img", "Ed25519_Performance_large"));

Summary ed3 = BenchmarkRunner.Run<AllNBenchmarksEd25519>(testConfig);
GenerateChart(ed3, outputPath,Path.Combine("img", "Ed25519_Performance_all"));
*/

/*
Summary x1 = BenchmarkRunner.Run<SmallNBenchmarksX25519>(testConfig);
GenerateChart(x1,  outputPath, Path.Combine("img", "X25519_Performance_small"));

Summary x2 = BenchmarkRunner.Run<LargeNBenchmarksX25519>(testConfig);
GenerateChart(x2,  outputPath, Path.Combine("img", "X25519_Performance_large"));
*/

Summary x3 = BenchmarkRunner.Run<AllNBenchmarksX25519>(testConfig);
GenerateChart(x3,  outputPath, Path.Combine("img", "ALL_X25519_Performance"));

Console.WriteLine($"\nThat's been done, out in:: {outputPath}");

if (OperatingSystem.IsWindows())
{
    System.Diagnostics.Process.Start("explorer.exe", outputPath);
}
