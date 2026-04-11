using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Benchmarks.Models;

public class CustomConfig : ManualConfig
{
    public CustomConfig(string outputPath)
    {
        this.ArtifactsPath = outputPath;
        
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddDiagnoser(MemoryDiagnoser.Default); 
        AddLogger(ConsoleLogger.Default);
        
        AddExporter(CsvExporter.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvMeasurementsExporter.Default); // Сырые данные (все 100 точек)
        
        // Настраиваем Job: 1 процесс, 5 прогревов, 100 итераций измерений
        AddJob(Job.Default
                .WithAffinity((IntPtr)2) 
                .WithLaunchCount(1)        // 1 изолированный процесс для теста
                .WithWarmupCount(5)        // 5 итераций на прогрев JIT
                .WithIterationCount(50)
                .WithGcForce(true)
                .AsDefault()
        );
    }
}

//dotnet run -c Release -- "C:\Users\yashelter\Desktop\DotnetCurve25519\OutLogs\" 