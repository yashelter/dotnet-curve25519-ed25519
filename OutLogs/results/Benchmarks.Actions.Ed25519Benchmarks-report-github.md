```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MVWXVL : .NET 10.0.5, X64 NativeAOT x86-64-v3

Force=True  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method                      | N      | Mean    | Error    | StdDev   | Ratio | RatioSD | Gen0        | Allocated   | Alloc Ratio |
|---------------------------- |------- |--------:|---------:|---------:|------:|--------:|------------:|------------:|------------:|
| **BouncyCastle_PureScalarMult** | **30000**  | **2.586 s** | **0.1495 s** | **0.0388 s** |  **1.00** |    **0.02** |   **2000.0000** |  **83280048 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 30000  | 1.706 s | 0.2004 s | 0.0520 s |  0.66 |    0.02 |           - |        48 B |       0.000 |
|                             |        |         |          |          |       |         |             |             |             |
| **BouncyCastle_PureScalarMult** | **50000**  | **4.474 s** | **0.4015 s** | **0.1043 s** |  **1.00** |    **0.03** |   **3000.0000** | **138800000 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 50000  | 2.845 s | 0.3065 s | 0.0796 s |  0.64 |    0.02 |           - |           - |        0.00 |
|                             |        |         |          |          |       |         |             |             |             |
| **BouncyCastle_PureScalarMult** | **60000**  | **5.161 s** | **0.0656 s** | **0.0170 s** |  **1.00** |    **0.00** |  **63000.0000** | **166560000 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 60000  | 3.370 s | 0.2886 s | 0.0750 s |  0.65 |    0.01 |           - |           - |        0.00 |
|                             |        |         |          |          |       |         |             |             |             |
| **BouncyCastle_PureScalarMult** | **80000**  | **6.891 s** | **0.2528 s** | **0.0391 s** |  **1.00** |    **0.01** |  **84000.0000** | **222080048 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 80000  | 4.691 s | 0.1594 s | 0.0414 s |  0.68 |    0.01 |           - |           - |        0.00 |
|                             |        |         |          |          |       |         |             |             |             |
| **BouncyCastle_PureScalarMult** | **100000** | **8.579 s** | **0.2871 s** | **0.0745 s** |  **1.00** |    **0.01** | **106000.0000** | **277600000 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 100000 | 7.429 s | 0.2439 s | 0.0377 s |  0.87 |    0.01 |           - |        48 B |       0.000 |
