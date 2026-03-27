```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MVWXVL : .NET 10.0.5, X64 NativeAOT x86-64-v3

Force=True  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method                      | N     | Mean         | Error       | StdDev      | Ratio | RatioSD | Gen0       | Allocated   | Alloc Ratio |
|---------------------------- |------ |-------------:|------------:|------------:|------:|--------:|-----------:|------------:|------------:|
| **BouncyCastle_PureScalarMult** | **16**    |     **1.508 ms** |   **0.3155 ms** |   **0.0819 ms** |  **1.00** |    **0.07** |          **-** |     **44417 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 16    |     1.690 ms |   0.3134 ms |   0.0814 ms |  1.12 |    0.07 |          - |         1 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **100**   |     **9.474 ms** |   **3.1729 ms** |   **0.8240 ms** |  **1.01** |    **0.11** |          **-** |    **277610 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 100   |    11.025 ms |   3.8974 ms |   1.0121 ms |  1.17 |    0.14 |          - |         6 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **1000**  |   **100.266 ms** |  **23.4102 ms** |   **6.0795 ms** |  **1.00** |    **0.08** |          **-** |   **2776112 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 1000  |   105.649 ms |  40.3942 ms |  10.4902 ms |  1.06 |    0.12 |          - |        77 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **5000**  |   **456.271 ms** | **108.0920 ms** |  **16.7274 ms** |  **1.00** |    **0.05** |          **-** |  **13880672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 5000  |   556.812 ms | 246.4022 ms |  63.9899 ms |  1.22 |    0.14 |          - |       672 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **10000** |   **893.977 ms** | **178.5645 ms** |  **46.3726 ms** |  **1.00** |    **0.07** |          **-** |  **27760672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 10000 | 1,003.090 ms | 158.7824 ms |  24.5718 ms |  1.12 |    0.06 |          - |       672 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **20000** | **1,812.855 ms** | **147.3083 ms** |  **38.2555 ms** |  **1.00** |    **0.03** | **21000.0000** |  **55520672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 20000 | 1,900.056 ms | 223.3634 ms |  58.0068 ms |  1.05 |    0.04 |          - |       672 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **30000** | **2,635.848 ms** | **338.3545 ms** |  **52.3607 ms** |  **1.00** |    **0.03** |  **2000.0000** |  **83280384 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 30000 | 3,138.342 ms | 211.1151 ms |  32.6703 ms |  1.19 |    0.02 |          - |       384 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **40000** | **3,463.472 ms** | **150.1801 ms** |  **23.2405 ms** |  **1.00** |    **0.01** | **42000.0000** | **111040672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 40000 | 4,611.761 ms | 758.4081 ms | 196.9562 ms |  1.33 |    0.05 |          - |       672 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **50000** | **4,267.749 ms** | **231.6535 ms** |  **35.8486 ms** |  **1.00** |    **0.01** |  **3000.0000** | **138800672 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 50000 | 5,304.684 ms | 589.0193 ms | 152.9665 ms |  1.24 |    0.03 |          - |       672 B |       0.000 |
|                             |       |              |             |             |       |         |            |             |             |
| **BouncyCastle_PureScalarMult** | **60000** | **5,209.091 ms** | **226.8393 ms** |  **58.9095 ms** |  **1.00** |    **0.01** | **63000.0000** | **166560384 B** |       **1.000** |
| CustomAvx2_PureScalarMult4  | 60000 | 6,039.860 ms | 535.9930 ms |  82.9455 ms |  1.16 |    0.02 |          - |       384 B |       0.000 |
