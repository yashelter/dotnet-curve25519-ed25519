```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MVWXVL : .NET 10.0.5, X64 NativeAOT x86-64-v3

Force=True  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method                      | N    | Mean         | Error        | StdDev      | Ratio | RatioSD | Allocated  | Alloc Ratio |
|---------------------------- |----- |-------------:|-------------:|------------:|------:|--------:|-----------:|------------:|
| **BouncyCastle_PureScalarMult** | **16**   |   **1,342.0 μs** |     **66.28 μs** |    **17.21 μs** |  **1.00** |    **0.02** |    **44416 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 16   |     834.3 μs |     42.24 μs |    10.97 μs |  0.62 |    0.01 |          - |        0.00 |
|                             |      |              |              |             |       |         |            |             |
| **BouncyCastle_PureScalarMult** | **100**  |   **8,376.4 μs** |    **472.84 μs** |   **122.80 μs** |  **1.00** |    **0.02** |   **277610 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 100  |   5,181.4 μs |    788.04 μs |   204.65 μs |  0.62 |    0.02 |          - |        0.00 |
|                             |      |              |              |             |       |         |            |             |
| **BouncyCastle_PureScalarMult** | **1000** |  **85,054.8 μs** |  **7,689.12 μs** | **1,996.84 μs** |  **1.00** |    **0.03** |  **2776112 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 1000 |  52,816.4 μs |  4,526.57 μs | 1,175.54 μs |  0.62 |    0.02 |          - |        0.00 |
|                             |      |              |              |             |       |         |            |             |
| **BouncyCastle_PureScalarMult** | **5000** | **423,370.4 μs** | **19,311.89 μs** | **2,988.54 μs** |  **1.00** |    **0.01** | **13880000 B** |        **1.00** |
| CustomAvx2_PureScalarMult4  | 5000 | 260,641.8 μs | 22,611.34 μs | 5,872.09 μs |  0.62 |    0.01 |          - |        0.00 |
