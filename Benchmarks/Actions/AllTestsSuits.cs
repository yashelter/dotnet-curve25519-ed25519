using BenchmarkDotNet.Attributes;

namespace Benchmarks.Actions;

public class SmallNBenchmarksEd25519 : Ed25519BenchmarksBase
{
    [Params(32, 64, 256, 1024)]
    public override int N { get; set; }
}

public class LargeNBenchmarksEd25519 : Ed25519BenchmarksBase
{
    [Params(30000, 40000, 50000, 100000)]
    public override int N { get; set; }
}

public class AllNBenchmarksEd25519 : Ed25519BenchmarksBase
{
    [Params(20000, 30000, 40000, 50000, 100000, 200000, 400000, 800000)]
    public override int N { get; set; }
}


public class SmallNBenchmarksX25519 : X25519BenchmarksBase
{
    [Params(32, 64, 256, 1024)]
    public override int N { get; set; }
}

public class LargeNBenchmarksX25519 : X25519BenchmarksBase
{
    [Params(30000, 40000, 50000, 100000)]
    public override int N { get; set; }
}

public class AllNBenchmarksX25519 : X25519BenchmarksBase
{
    [Params(40000, 50000, 100000, 200000, 300000, 400000, 500000, 600000, 700000, 800000, 1000000)]
    public override int N { get; set; }
}