using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarks.Actions;
using Benchmarks.Models;
using static Benchmarks.Helpers.ChartGenerator;

string outputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkResults");

CustomConfig testConfig = new (outputPath);

Console.WriteLine("Начинаем тестирование Ed25519...");
Summary edSummary = BenchmarkRunner.Run<Ed25519Benchmarks>(testConfig);

Console.WriteLine("Начинаем тестирование X25519...");
Summary xSummary = BenchmarkRunner.Run<X25519Benchmarks>(testConfig);

GenerateChart(edSummary, outputPath,"Ed25519_Performance");
GenerateChart(xSummary,  outputPath, "X25519_Performance");

Console.WriteLine($"\nThat's been done, out in:: {outputPath}");

if (OperatingSystem.IsWindows())
{
    System.Diagnostics.Process.Start("explorer.exe", outputPath);
}
