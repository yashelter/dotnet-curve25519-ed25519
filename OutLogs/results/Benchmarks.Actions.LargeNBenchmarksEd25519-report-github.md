```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MNACAZ : .NET 10.0.5, X64 NativeAOT x86-64-v3

Affinity=0000000000000010  Force=True  IterationCount=50  
LaunchCount=1  WarmupCount=5  

```
| Method                      | N      | Mean    | Error    | StdDev   | Median  | Ratio | RatioSD | Gen0       | Allocated   | Alloc Ratio |
|---------------------------- |------- |--------:|---------:|---------:|--------:|------:|--------:|-----------:|------------:|------------:|
| **BouncyCastle_PureScalarMult** | **30000**  | **2.502 s** | **0.0048 s** | **0.0093 s** | **2.497 s** |  **1.00** |    **0.01** |  **4000.0000** |  **83280672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 30000  | 1.531 s | 0.0006 s | 0.0010 s | 1.531 s |  0.61 |    0.00 |          - |       672 B |       0.000 |
|                             |        |         |          |          |         |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **40000**  | **3.322 s** | **0.0059 s** | **0.0104 s** | **3.327 s** |  **1.00** |    **0.00** |  **6000.0000** | **111040672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 40000  | 2.059 s | 0.0007 s | 0.0012 s | 2.059 s |  0.62 |    0.00 |          - |       672 B |       0.000 |
|                             |        |         |          |          |         |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **50000**  | **4.133 s** | **0.0044 s** | **0.0086 s** | **4.129 s** |  **1.00** |    **0.00** |  **8000.0000** | **138800672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 50000  | 2.563 s | 0.0244 s | 0.0434 s | 2.552 s |  0.62 |    0.01 |          - |       672 B |       0.000 |
|                             |        |         |          |          |         |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **100000** | **8.730 s** | **0.1268 s** | **0.2473 s** | **8.695 s** |  **1.00** |    **0.04** | **16000.0000** | **277600000 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 100000 | 5.641 s | 0.0835 s | 0.1647 s | 5.633 s |  0.65 |    0.03 |          - |       672 B |       0.000 |
