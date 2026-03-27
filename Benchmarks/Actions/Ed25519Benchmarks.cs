using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Core.Ed25519;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace Benchmarks.Actions;

[MemoryDiagnoser]
public class Ed25519Benchmarks
{
    [Params(16, 100, 1000, 5000, 10000, 20000, 30000, 40000, 50000, 60000)]  // кратно 4
    public int N { get; set; } 
    
    private byte[] _randomScalars = null!;
    private byte[] _basePointBytes = null!;
    private PointExt4 _basePointBatch;
    
    private byte[] _outputBc = null!;
    
    // Данные для BouncyCastle
    private Ed25519.PointAffine _bcBasePoint;
    
    // Данные для Custom AVX2
    private Core.Ed25519.PointExt4 _customBasePointBatch;
    private Core.Ed25519.PointExt4 _outputCustom;
    
    
    [GlobalSetup]
    public void Setup()
    {
        _randomScalars = new byte[N * 32];
        RandomNumberGenerator.Fill(_randomScalars);
        
        // "Причесываем" скаляры для Ed25519
        for (int i = 0; i < N; i++)
        {
            int offset = i * 32;
            _randomScalars[offset] &= 248;
            _randomScalars[offset + 31] &= 127;
            _randomScalars[offset + 31] |= 64;
        }
        
        _basePointBytes = new byte[32];
        Ed25519.GeneratePublicKey(new byte[32], 0, _basePointBytes, 0);
        
        _bcBasePoint.x = X25519Field.Create();
        _bcBasePoint.y = X25519Field.Create();
        
        Ed25519.DecodePointVar(_basePointBytes, false, ref _bcBasePoint);
        
        _customBasePointBatch = PrepareBasePointBatch();
    }

    [Benchmark(Baseline = true)]
    public void BouncyCastle_PureScalarMult()
    {
        ReadOnlySpan<byte> scalars = _randomScalars;
        for (int i = 0; i < N; i++)
        {
            Ed25519.PointAccum r;
            Ed25519.Init(out r);
            
            Ed25519.ScalarMult(scalars.Slice(i * 32, 32), ref _bcBasePoint, ref r);
        }
    }

    [Benchmark]
    public void CustomAvx2_PureScalarMult4()
    {
        ReadOnlySpan<byte> scalars = _randomScalars;
        for (int i = 0; i < N; i += 4)
        {
            Ed25519Batch.Multiply4(scalars.Slice(i * 32, 128), in _customBasePointBatch, out _outputCustom);
        }
    }

    private static PointExt4 PrepareBasePointBatch()
    {
        ReadOnlySpan<byte> bx = new byte[32] {
            0x1a, 0xd5, 0x25, 0x8f, 0x60, 0x2d, 0x56, 0xc9, 0xb2, 0xa7, 0x25, 0x95, 0x60, 0xc7, 0x2c, 0x69,
            0x5c, 0xdc, 0xd6, 0xfd, 0x31, 0xe2, 0xa4, 0xc0, 0xfe, 0x53, 0x6e, 0xcd, 0xd3, 0x36, 0x69, 0x21
        };
        ReadOnlySpan<byte> by = new byte[32] {
            0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66,
            0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66
        };
        
        Span<byte> x128 = stackalloc byte[128];
        Span<byte> y128 = stackalloc byte[128];
        
        for (int i = 0; i < 4; i++)
        {
            bx.CopyTo(x128.Slice(i * 32, 32));
            by.CopyTo(y128.Slice(i * 32, 32));
        }
        
        Ed25519Batch.FromAffine(out var point,
            FieldElement4.FromBytes(x128),
            FieldElement4.FromBytes(y128));
        
        return point;
    }
}