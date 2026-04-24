```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MNACAZ : .NET 10.0.5, X64 NativeAOT x86-64-v3

Affinity=0000000000000010  Force=True  IterationCount=50  
LaunchCount=1  WarmupCount=5  

```
| Method                      | N    | Mean      | Error     | StdDev    | Median    | Ratio | Gen0     | Allocated | Alloc Ratio |
|---------------------------- |----- |----------:|----------:|----------:|----------:|------:|---------:|----------:|------------:|
| **BouncyCastle_PureScalarMult** | **32**   |  **2.677 ms** | **0.0098 ms** | **0.0188 ms** |  **2.669 ms** |  **1.00** |   **3.9063** |   **88835 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 32   |  1.835 ms | 0.0041 ms | 0.0078 ms |  1.832 ms |  0.69 |        - |       1 B |       0.000 |
|                             |      |           |           |           |           |       |          |           |             |
| **BouncyCastle_PureScalarMult** | **64**   |  **5.358 ms** | **0.0209 ms** | **0.0407 ms** |  **5.344 ms** |  **1.00** |   **7.8125** |  **177669 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 64   |  3.676 ms | 0.0282 ms | 0.0569 ms |  3.659 ms |  0.69 |        - |       3 B |       0.000 |
|                             |      |           |           |           |           |       |          |           |             |
| **BouncyCastle_PureScalarMult** | **256**  | **21.463 ms** | **0.1169 ms** | **0.2279 ms** | **21.340 ms** |  **1.00** |  **31.2500** |  **710677 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 256  | 13.421 ms | 0.0070 ms | 0.0121 ms | 13.421 ms |  0.63 |        - |      10 B |       0.000 |
|                             |      |           |           |           |           |       |          |           |             |
| **BouncyCastle_PureScalarMult** | **1024** | **85.262 ms** | **0.0649 ms** | **0.1154 ms** | **85.264 ms** |  **1.00** | **166.6667** | **2842736 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 1024 | 58.928 ms | 0.1846 ms | 0.3329 ms | 58.829 ms |  0.69 |        - |      75 B |       0.000 |
