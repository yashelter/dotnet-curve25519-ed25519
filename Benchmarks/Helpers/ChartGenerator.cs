using System.Globalization;
using BenchmarkDotNet.Reports;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Stylers;

namespace Benchmarks.Helpers;

public static class ChartGenerator
{
    public static void GenerateChart(Summary summary, string outPath, string title)
    {
        GenerateChartOld(summary, outPath, title);
        GenerateAdvancedCharts(summary, outPath, title);
    }
    
    private static void GenerateChartOld(Summary summary, string outPath, string title)
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
        bcScatter.LegendText = "BouncyCastle";
        bcScatter.LineWidth = 3;
        bcScatter.MarkerSize = 8;
        
        Scatter customScatter = myPlot.Add.Scatter(logXs, customTimes);
        customScatter.LegendText = "AVX2 Batch";
        customScatter.LineWidth = 3;
        customScatter.MarkerSize = 8;
        
        myPlot.Title(title);
        myPlot.XLabel("Количество операций (пар ключей)");
        myPlot.YLabel("Время выполнения (ms) - Меньше значит лучше");
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
        myPlot.SavePng(filePath, 1200, 800);
        
        Console.WriteLine($"График сохранен: {filePath}");
    }
    
    
    public static void GenerateAdvancedCharts(Summary summary, string outPath, string title)
    {
        int[] nValues = summary.Reports
            .Select(r => (int)r.BenchmarkCase.Parameters["N"])
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        double[] logXs = nValues.Select(n => Math.Log10((double)n)).ToArray();
        
        double[] bcTimes = new double[nValues.Length];
        double[] customTimes = new double[nValues.Length];

        IEnumerable<BenchmarkReport> reports = summary.Reports.AsEnumerable();

        for (int i = 0; i < nValues.Length; i++)
        {
            int currentN = nValues[i];
            
            var bcReport = reports.First(r => 
                (int)r.BenchmarkCase.Parameters["N"] == currentN && 
                r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("BouncyCastle"));
                
            var customReport = reports.First(r => 
                (int)r.BenchmarkCase.Parameters["N"] == currentN && 
                r.BenchmarkCase.Descriptor.WorkloadMethod.Name.Contains("CustomAvx2"));

            bcTimes[i] = bcReport.ResultStatistics!.Mean / 1_000_000.0;
            customTimes[i] = customReport.ResultStatistics!.Mean / 1_000_000.0;
        }

        Plot straightPlot = CreateBasePlot(title, logXs, bcTimes, customTimes, nValues, smooth: false);
        string pathStraight = Path.Combine(outPath, $"{title}_Improved_Straight.png");
        straightPlot.SavePng(pathStraight, 1200, 800);
        Console.WriteLine($"Улучшенный график сохранен: {pathStraight}");

        Plot smoothPlot = CreateBasePlot(title + " (Smooth Curves)", logXs, bcTimes, customTimes, nValues, smooth: true);
        string pathSmooth = Path.Combine(outPath, $"{title}_Improved_Smooth.png");
        smoothPlot.SavePng(pathSmooth, 1200, 800);
        
        Console.WriteLine($"Плавный график сохранен: {pathSmooth}");
    }

    private static Plot CreateBasePlot(string title, double[] xs, double[] bcTimes, double[] customTimes, int[] originalN, bool smooth)
    {
        Plot myPlot = new();
        
        double[] speedups = new double[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            speedups[i] = bcTimes[i] / customTimes[i];
        }
        double avgSpeedup = speedups.Average();

        // Добавляем заливку между линиями (выделяем выигрыш в производительности)
        // Мы заливаем область от Custom до BouncyCastle
        var fill = myPlot.Add.FillY(xs, bcTimes, customTimes);
        fill.FillColor = Colors.SteelBlue.WithAlpha(0.2); // Полупрозрачный синий
        fill.FillHatch = new ScottPlot.Hatches.Striped(); // Можно добавить штриховку для стиля

        // Добавляем BouncyCastle (Красный)
        var bcScatter = myPlot.Add.Scatter(xs, bcTimes);
        bcScatter.LegendText = $"BouncyCastle (Base)";
        bcScatter.LineWidth = 3;
        bcScatter.MarkerSize = 8;
        bcScatter.Color = Colors.IndianRed; 
        bcScatter.Smooth = smooth;

        // Добавляем CustomAvx2 (Синий)
        var customScatter = myPlot.Add.Scatter(xs, customTimes);
        customScatter.LegendText = $"AVX2 Batch ({avgSpeedup:F1}x faster)";
        customScatter.LineWidth = 3;
        customScatter.MarkerSize = 8;
        customScatter.Color = Colors.SteelBlue; 
        customScatter.Smooth = smooth;
        
        
        for (int i = 0; i < xs.Length; i++)
        {
            var txt = myPlot.Add.Text($"{speedups[i]:F1}x", xs[i], customTimes[i]);
            txt.LabelFontSize = 14;
            txt.LabelBold = true;
            txt.LabelFontColor = Colors.SteelBlue;
            txt.LabelAlignment = Alignment.UpperCenter;
            txt.LabelOffsetY = 10;
        }

        myPlot.Title(title);
        myPlot.XLabel("Количество операций");
        myPlot.YLabel("Время выполнения (ms)");
        myPlot.ShowLegend(Alignment.UpperLeft); 

        Tick[] xTicks = new Tick[originalN.Length];
        for (int i = 0; i < originalN.Length; i++)
        {
            xTicks[i] = new Tick(xs[i], FormatNumberToK(originalN[i]));
        }
        myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(xTicks);
        myPlot.Axes.Bottom.TickLabelStyle.Rotation = -45;
        
        double maxTime = Math.Max(bcTimes.Max(), customTimes.Max());
        myPlot.Axes.SetLimitsY(0, maxTime * 1.25);

        myPlot.Grid.MajorLineColor = Colors.LightGray;

        return myPlot;
    }

    private static string FormatNumberToK(int number)
    {
        if (number >= 1_000_000)
            return (number / 1_000_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            
        if (number >= 1_000)
            return (number / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            
        return number.ToString(CultureInfo.InvariantCulture);
    }
}