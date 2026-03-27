using System.Globalization;
using BenchmarkDotNet.Reports;
using ScottPlot;
using ScottPlot.Plottables;

namespace Benchmarks.Helpers;

public static class ChartGenerator
{
    public static void GenerateChart(Summary summary, string outPath, string title)
    {
        int[] nValues = summary.Reports
            .Select(r => (int)r.BenchmarkCase.Parameters["N"])
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        double[] xs = nValues.Select(n => (double)n).ToArray();
        
        double[] logXs = xs.Select(Math.Log10).ToArray();
        
        double[] bcTimes = new double[nValues.Length];
        double[] customTimes = new double[nValues.Length];

        IEnumerable<BenchmarkReport> reports = summary.Reports.AsEnumerable();

        for (int i = 0; i < nValues.Length; i++)
        {
            int currentN = nValues[i];
            
            BenchmarkReport bcReport = reports.First(r => 
                (int)r.BenchmarkCase.Parameters["N"] == currentN && 
                r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("BouncyCastle"));
                
            BenchmarkReport customReport = reports.First(r => 
                (int)r.BenchmarkCase.Parameters["N"] == currentN && 
                r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("CustomAvx2"));

            bcTimes[i] = bcReport.ResultStatistics!.Mean / 1_000_000.0;
            customTimes[i] = customReport.ResultStatistics!.Mean / 1_000_000.0;
        }

        Plot myPlot = new();

        Scatter bcScatter = myPlot.Add.Scatter(logXs, bcTimes);
        bcScatter.LegendText = "BouncyCastle (Scalar)";
        bcScatter.LineWidth = 3;
        bcScatter.MarkerSize = 8;

        Scatter customScatter = myPlot.Add.Scatter(logXs, customTimes);
        customScatter.LegendText = "Custom Engine (AVX2 Batch)";
        customScatter.LineWidth = 3;
        customScatter.MarkerSize = 8;

        myPlot.Title(title);
        myPlot.XLabel("Количество операций (пар ключей)");
        myPlot.YLabel("Время выполнения (Миллисекунды) - Меньше значит лучше");
        myPlot.ShowLegend();

        // Вручную создаем кастомные подписи для оси X. 
        // График нарисован по LogX (например, 1, 2, 3, 4, 5), 
        // но мы заменяем тексты подписей на реальные (10, 100, 1000, 10000, 100000).
        Tick[] xTicks = new Tick[nValues.Length];
        for (int i = 0; i < nValues.Length; i++)
        {
            xTicks[i] = new Tick(logXs[i], xs[i].ToString(CultureInfo.InvariantCulture));
        }
        myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(xTicks);

        // Сохраняем файл
        string filePath = Path.Combine(outPath, $"{title}_Chart.png");
        myPlot.SavePng(filePath, 1000, 800);
        
        Console.WriteLine($"График сохранен: {filePath}");
    }
}