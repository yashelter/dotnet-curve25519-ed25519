using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Core.Base;

namespace Core.Ed25519;

public struct PointExt4
{
    public FieldElement4 X, Y, Z, T;
    
    public static readonly PointExt4 Zero = new PointExt4
    {
        X = FieldElement4.Zero,
        Y = FieldElement4.One,
        Z = FieldElement4.One,
        T = FieldElement4.Zero
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SelectField(ref FieldElement4 dest, in FieldElement4 a, in FieldElement4 b, Vector256<byte> mask)
    {
        // Если mask == 0xFF..FF, берем b. Если 0x00..00, берем a.
        dest.L0 = Avx2.BlendVariable(a.L0.AsByte(), b.L0.AsByte(), mask).AsUInt64();
        dest.L1 = Avx2.BlendVariable(a.L1.AsByte(), b.L1.AsByte(), mask).AsUInt64();
        dest.L2 = Avx2.BlendVariable(a.L2.AsByte(), b.L2.AsByte(), mask).AsUInt64();
        dest.L3 = Avx2.BlendVariable(a.L3.AsByte(), b.L3.AsByte(), mask).AsUInt64();
        dest.L4 = Avx2.BlendVariable(a.L4.AsByte(), b.L4.AsByte(), mask).AsUInt64();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CSelect(ref PointExt4 dest, in PointExt4 a, in PointExt4 b, Vector256<ulong> maskBits)
    {
        var mask = maskBits.AsByte();
        SelectField(ref dest.X, in a.X, in b.X, mask);
        SelectField(ref dest.Y, in a.Y, in b.Y, mask);
        SelectField(ref dest.Z, in a.Z, in b.Z, mask);
        SelectField(ref dest.T, in a.T, in b.T, mask);
    }
    
    public static readonly PointExt4 BasePoint = CreateBasePoint();
    
    private static PointExt4 CreateBasePoint()
    {
        // Y = 4/5 mod p
        ReadOnlySpan<byte> by = new byte[32] {
            0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66,
            0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66
        };
        // X вычисляется из уравнения кривой для Y
        ReadOnlySpan<byte> bx = new byte[32] {
            0x1a, 0xd5, 0x25, 0x8f, 0x60, 0x2d, 0x56, 0xc9, 0xb2, 0xa7, 0x25, 0x95, 0x60, 0xc7, 0x2c, 0x69,
            0x5c, 0xdc, 0xd6, 0xfd, 0x31, 0xe2, 0xa4, 0xc0, 0xfe, 0x53, 0x6e, 0xcd, 0xd3, 0x36, 0x69, 0x21
        };
        
        Span<byte> x128 = stackalloc byte[128];
        Span<byte> y128 = stackalloc byte[128];
        for (int i = 0; i < 4; i++) {
            bx.CopyTo(x128.Slice(i * 32, 32));
            by.CopyTo(y128.Slice(i * 32, 32));
        }
        
        Ed25519Batch.FromAffine(out var point, FieldElement4.FromBytes(x128), FieldElement4.FromBytes(y128));
        return point;
    }
    
    public void EncodeToBytes(Span<byte> output)
    {
        Ed25519Batch.ToAffine(in this, out var x, out var y);
        y.ToBytes(output); // Сохраняем Y
        
        Span<byte> xBytes = stackalloc byte[128];
        x.ToBytes(xBytes);
        
        // По стандарту RFC8032: верхний бит последнего байта Y содержит младший бит X (четность)
        output[31] |= (byte)((xBytes[0] & 1) << 7);
        output[63] |= (byte)((xBytes[32] & 1) << 7);
        output[95] |= (byte)((xBytes[64] & 1) << 7);
        output[127] |= (byte)((xBytes[96] & 1) << 7);
    }
}