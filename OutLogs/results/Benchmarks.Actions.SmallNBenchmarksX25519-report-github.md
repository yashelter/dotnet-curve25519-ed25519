```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8037/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5700X 3.40GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.5, X64 NativeAOT x86-64-v3
  Job-MNACAZ : .NET 10.0.5, X64 NativeAOT x86-64-v3

Affinity=0000000000000010  Force=True  IterationCount=50  
LaunchCount=1  WarmupCount=5  

```
| Method              | N    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |----- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **BouncyCastle_X25519** | **32**   |  **2.975 ms** | **0.0810 ms** | **0.1617 ms** |  **1.00** |    **0.08** |   **28419 B** |       **1.000** |
| CustomAvx2_X25519   | 32   |  2.340 ms | 0.0513 ms | 0.1001 ms |  0.79 |    0.05 |       3 B |       0.000 |
|                     |      |           |           |           |       |         |           |             |
| **BouncyCastle_X25519** | **64**   |  **5.719 ms** | **0.0950 ms** | **0.1875 ms** |  **1.00** |    **0.05** |   **56837 B** |       **1.000** |
| CustomAvx2_X25519   | 64   |  3.364 ms | 0.0892 ms | 0.1781 ms |  0.59 |    0.04 |       3 B |       0.000 |
|                     |      |           |           |           |       |         |           |             |
| **BouncyCastle_X25519** | **256**  | **23.340 ms** | **0.6079 ms** | **1.2281 ms** |  **1.00** |    **0.07** |  **227328 B** |       **1.000** |
| CustomAvx2_X25519   | 256  | 20.196 ms | 0.3925 ms | 0.7838 ms |  0.87 |    0.06 |      21 B |       0.000 |
|                     |      |           |           |           |       |         |           |             |
| **BouncyCastle_X25519** | **1024** | **95.265 ms** | **2.0523 ms** | **4.1458 ms** |  **1.00** |    **0.06** |  **909312 B** |       **1.000** |
| CustomAvx2_X25519   | 1024 | 74.210 ms | 1.5637 ms | 3.1228 ms |  0.78 |    0.05 |      75 B |       0.000 |
