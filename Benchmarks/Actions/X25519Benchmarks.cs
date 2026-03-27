using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

using BcX25519 = Org.BouncyCastle.Math.EC.Rfc7748.X25519;

namespace Benchmarks.Actions;

[MemoryDiagnoser]
public class X25519Benchmarks
{
    [Params(16, 100, 1000, 5000, 10000, 20000, 30000, 40000, 50000, 60000)]  // кратно 4
    public int N { get; set; }
    
    private byte[] _randomKeys = null!;
    private byte[] _outputBufferBc = null!;
    private byte[] _outputBufferCustom= null!;
    private byte[] _basePointBc = null!;
    private byte[] _basePointBufferCustom = null!;
    
    
    [GlobalSetup]
    public void Setup()
    {
        _randomKeys = new byte[this.N * 32];
        RandomNumberGenerator.Fill(_randomKeys);
        
        _outputBufferBc = new byte[32];
        _outputBufferCustom = new byte[128];
        
        _basePointBc = new byte[32];
        RandomNumberGenerator.Fill(_basePointBc);
        
        _basePointBufferCustom = new byte[128];
        _basePointBc.CopyTo(_basePointBufferCustom, 0);
        _basePointBc.CopyTo(_basePointBufferCustom, 32);
        _basePointBc.CopyTo(_basePointBufferCustom, 64);
        _basePointBc.CopyTo(_basePointBufferCustom, 96);
    }
    
    [Benchmark(Baseline = true)]
    public void BouncyCastle_X25519()
    {
        for (int i = 0; i < this.N; i++)
        {
            BcX25519.ScalarMult(_randomKeys, i * 32, _basePointBc, 0, _outputBufferBc, 0);
        }
    }
    
    [Benchmark]
    public void CustomAvx2_X25519()
    {
        ReadOnlySpan<byte> keys = _randomKeys;
        for (int i = 0; i < (this.N / 4); i++)
        {
            X25519Batch.Multiply4( keys.Slice(i * 128, 128), _basePointBufferCustom, _outputBufferCustom);
        }
    }
}
