using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Core.Math;

[StructLayout(LayoutKind.Sequential)]
public struct BatchedFieldElement(
    Vector256<ulong> l0,
    Vector256<ulong> l1,
    Vector256<ulong> l2,
    Vector256<ulong> l3,
    Vector256<ulong> l4)
{
    public Vector256<ulong> L0 = l0;
    public Vector256<ulong> L1 = l1;
    public Vector256<ulong> L2 = l2;
    public Vector256<ulong> L3 = l3;
    public Vector256<ulong> L4 = l4;

    private const ulong MASK51 = (1UL << 51) - 1;
    
    public static readonly BatchedFieldElement Zero = new BatchedFieldElement(
        Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly BatchedFieldElement One = new BatchedFieldElement(
        Vector256.Create(1UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly BatchedFieldElement A24 = new BatchedFieldElement(
        Vector256.Create(121665UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(ref BatchedFieldElement r, in BatchedFieldElement a, in BatchedFieldElement b)
    {
        r.L0 = a.L0 + b.L0;
        r.L1 = a.L1 + b.L1;
        r.L2 = a.L2 + b.L2;
        r.L3 = a.L3 + b.L3;
        r.L4 = a.L4 + b.L4;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sub(ref BatchedFieldElement r, in BatchedFieldElement a, in BatchedFieldElement b)
    {
        Vector256<ulong> v2p0 = Vector256.Create(0xFFFFFFFFFFFDAUL);
        Vector256<ulong> v2p14 = Vector256.Create(0xFFFFFFFFFFFFEUL);

        r.L0 = (a.L0 + v2p0) - b.L0;
        r.L1 = (a.L1 + v2p14) - b.L1;
        r.L2 = (a.L2 + v2p14) - b.L2;
        r.L3 = (a.L3 + v2p14) - b.L3;
        r.L4 = (a.L4 + v2p14) - b.L4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CSwap(ref BatchedFieldElement a, ref BatchedFieldElement b, Vector256<ulong> mask)
    {
        Vector256<ulong> dummy0 = mask & (a.L0 ^ b.L0);
        Vector256<ulong> dummy1 = mask & (a.L1 ^ b.L1);
        Vector256<ulong> dummy2 = mask & (a.L2 ^ b.L2);
        Vector256<ulong> dummy3 = mask & (a.L3 ^ b.L3);
        Vector256<ulong> dummy4 = mask & (a.L4 ^ b.L4);

        a.L0 ^= dummy0; b.L0 ^= dummy0;
        a.L1 ^= dummy1; b.L1 ^= dummy1;
        a.L2 ^= dummy2; b.L2 ^= dummy2;
        a.L3 ^= dummy3; b.L3 ^= dummy3;
        a.L4 ^= dummy4; b.L4 ^= dummy4;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MulLane(
        ulong a0, ulong a1, ulong a2, ulong a3, ulong a4,
        ulong b0, ulong b1, ulong b2, ulong b3, ulong b4,
        out ulong r0, out ulong r1, out ulong r2, out ulong r3, out ulong r4)
    {
        ulong b1_19 = b1 * 19, b2_19 = b2 * 19, b3_19 = b3 * 19, b4_19 = b4 * 19;

        UInt128 res0 = (UInt128)a0 * b0 + (UInt128)a1 * b4_19 + (UInt128)a2 * b3_19 + (UInt128)a3 * b2_19 + (UInt128)a4 * b1_19;
        UInt128 res1 = (UInt128)a0 * b1 + (UInt128)a1 * b0   + (UInt128)a2 * b4_19 + (UInt128)a3 * b3_19 + (UInt128)a4 * b2_19;
        UInt128 res2 = (UInt128)a0 * b2 + (UInt128)a1 * b1   + (UInt128)a2 * b0   + (UInt128)a3 * b4_19 + (UInt128)a4 * b3_19;
        UInt128 res3 = (UInt128)a0 * b3 + (UInt128)a1 * b2   + (UInt128)a2 * b1   + (UInt128)a3 * b0   + (UInt128)a4 * b4_19;
        UInt128 res4 = (UInt128)a0 * b4 + (UInt128)a1 * b3   + (UInt128)a2 * b2   + (UInt128)a3 * b1   + (UInt128)a4 * b0;

        ulong c;
        ulong h0 = (ulong)(res0 & MASK51); c = (ulong)(res0 >> 51); res1 += c;
        ulong h1 = (ulong)(res1 & MASK51); c = (ulong)(res1 >> 51); res2 += c;
        ulong h2 = (ulong)(res2 & MASK51); c = (ulong)(res2 >> 51); res3 += c;
        ulong h3 = (ulong)(res3 & MASK51); c = (ulong)(res3 >> 51); res4 += c;
        ulong h4 = (ulong)(res4 & MASK51); c = (ulong)(res4 >> 51);

        h0 += c * 19;
        c = h0 >> 51; h0 &= MASK51; h1 += c;

        r0 = h0; r1 = h1; r2 = h2; r3 = h3; r4 = h4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(ref BatchedFieldElement r, in BatchedFieldElement a, in BatchedFieldElement b)
    {
        ref ulong a0 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L0));
        ref ulong a1 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L1));
        ref ulong a2 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L2));
        ref ulong a3 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L3));
        ref ulong a4 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L4));

        ref ulong b0 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L0));
        ref ulong b1 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L1));
        ref ulong b2 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L2));
        ref ulong b3 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L3));
        ref ulong b4 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L4));

        MulLane(a0, a1, a2, a3, a4, b0, b1, b2, b3, b4, 
            out ulong r0_0, out ulong r1_0, out ulong r2_0, out ulong r3_0, out ulong r4_0);
            
        MulLane(Unsafe.Add(ref a0, 1), Unsafe.Add(ref a1, 1), Unsafe.Add(ref a2, 1), Unsafe.Add(ref a3, 1), Unsafe.Add(ref a4, 1),
                Unsafe.Add(ref b0, 1), Unsafe.Add(ref b1, 1), Unsafe.Add(ref b2, 1), Unsafe.Add(ref b3, 1), Unsafe.Add(ref b4, 1), 
            out ulong r0_1, out ulong r1_1, out ulong r2_1, out ulong r3_1, out ulong r4_1);
            
        MulLane(Unsafe.Add(ref a0, 2), Unsafe.Add(ref a1, 2), Unsafe.Add(ref a2, 2), Unsafe.Add(ref a3, 2), Unsafe.Add(ref a4, 2),
                Unsafe.Add(ref b0, 2), Unsafe.Add(ref b1, 2), Unsafe.Add(ref b2, 2), Unsafe.Add(ref b3, 2), Unsafe.Add(ref b4, 2), 
            out ulong r0_2, out ulong r1_2, out ulong r2_2, out ulong r3_2, out ulong r4_2);
            
        MulLane(Unsafe.Add(ref a0, 3), Unsafe.Add(ref a1, 3), Unsafe.Add(ref a2, 3), Unsafe.Add(ref a3, 3), Unsafe.Add(ref a4, 3),
                Unsafe.Add(ref b0, 3), Unsafe.Add(ref b1, 3), Unsafe.Add(ref b2, 3), Unsafe.Add(ref b3, 3), Unsafe.Add(ref b4, 3), 
            out ulong r0_3, out ulong r1_3, out ulong r2_3, out ulong r3_3, out ulong r4_3);

        r.L0 = Vector256.Create(r0_0, r0_1, r0_2, r0_3);
        r.L1 = Vector256.Create(r1_0, r1_1, r1_2, r1_3);
        r.L2 = Vector256.Create(r2_0, r2_1, r2_2, r2_3);
        r.L3 = Vector256.Create(r3_0, r3_1, r3_2, r3_3);
        r.L4 = Vector256.Create(r4_0, r4_1, r4_2, r4_3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SqrLane(
        ulong f0, ulong f1, ulong f2, ulong f3, ulong f4,
        out ulong r0, out ulong r1, out ulong r2, out ulong r3, out ulong r4)
    {
        ulong f0_2 = f0 * 2, f1_2 = f1 * 2;
        ulong f3_19 = f3 * 19, f4_19 = f4 * 19;
        ulong f4_38 = f4 * 38, f3_38 = f3 * 38;

        UInt128 res0 = (UInt128)f0 * f0 + (UInt128)f1 * f4_38 + (UInt128)f2 * f3_38;
        UInt128 res1 = (UInt128)f0_2 * f1 + (UInt128)f2 * f4_38 + (UInt128)f3 * f3_19;
        UInt128 res2 = (UInt128)f0_2 * f2 + (UInt128)f1 * f1   + (UInt128)f3 * f4_38;
        UInt128 res3 = (UInt128)f0_2 * f3 + (UInt128)f1_2 * f2 + (UInt128)f4 * f4_19;
        UInt128 res4 = (UInt128)f0_2 * f4 + (UInt128)f1_2 * f3 + (UInt128)f2 * f2;

        ulong c;
        ulong h0 = (ulong)(res0 & MASK51); c = (ulong)(res0 >> 51); res1 += c;
        ulong h1 = (ulong)(res1 & MASK51); c = (ulong)(res1 >> 51); res2 += c;
        ulong h2 = (ulong)(res2 & MASK51); c = (ulong)(res2 >> 51); res3 += c;
        ulong h3 = (ulong)(res3 & MASK51); c = (ulong)(res3 >> 51); res4 += c;
        ulong h4 = (ulong)(res4 & MASK51); c = (ulong)(res4 >> 51);

        h0 += c * 19;
        c = h0 >> 51; h0 &= MASK51; h1 += c;

        r0 = h0; r1 = h1; r2 = h2; r3 = h3; r4 = h4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Square(ref BatchedFieldElement r, in BatchedFieldElement f)
    {
        ref ulong f0 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L0));
        ref ulong f1 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L1));
        ref ulong f2 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L2));
        ref ulong f3 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L3));
        ref ulong f4 = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L4));

        SqrLane(f0, f1, f2, f3, f4, out ulong r0_0, out ulong r1_0, out ulong r2_0, out ulong r3_0, out ulong r4_0);
        SqrLane(Unsafe.Add(ref f0, 1), Unsafe.Add(ref f1, 1), Unsafe.Add(ref f2, 1), Unsafe.Add(ref f3, 1), Unsafe.Add(ref f4, 1),
            out ulong r0_1, out ulong r1_1, out ulong r2_1, out ulong r3_1, out ulong r4_1);
        SqrLane(Unsafe.Add(ref f0, 2), Unsafe.Add(ref f1, 2), Unsafe.Add(ref f2, 2), Unsafe.Add(ref f3, 2), Unsafe.Add(ref f4, 2),
            out ulong r0_2, out ulong r1_2, out ulong r2_2, out ulong r3_2, out ulong r4_2);
        SqrLane(Unsafe.Add(ref f0, 3), Unsafe.Add(ref f1, 3), Unsafe.Add(ref f2, 3), Unsafe.Add(ref f3, 3), Unsafe.Add(ref f4, 3),
            out ulong r0_3, out ulong r1_3, out ulong r2_3, out ulong r3_3, out ulong r4_3);

        r.L0 = Vector256.Create(r0_0, r0_1, r0_2, r0_3);
        r.L1 = Vector256.Create(r1_0, r1_1, r1_2, r1_3);
        r.L2 = Vector256.Create(r2_0, r2_1, r2_2, r2_3);
        r.L3 = Vector256.Create(r3_0, r3_1, r3_2, r3_3);
        r.L4 = Vector256.Create(r4_0, r4_1, r4_2, r4_3);
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Invert(ref BatchedFieldElement r, in BatchedFieldElement z)
    {
        BatchedFieldElement _2 = default; Square(ref _2, in z);
        BatchedFieldElement _4 = default; Square(ref _4, in _2);
        BatchedFieldElement _8 = default; Square(ref _8, in _4);
        
        BatchedFieldElement _9 = default; Multiply(ref _9, in _8, in z);
        BatchedFieldElement _11 = default; Multiply(ref _11, in _9, in _2);
        BatchedFieldElement _22 = default; Square(ref _22, in _11);
        
        BatchedFieldElement _2_5_0 = default; Multiply(ref _2_5_0, in _22, in _9);
        
        BatchedFieldElement t = default; Square(ref t, in _2_5_0);
        for (int i = 0; i < 4; i++) Square(ref t, in t);
        BatchedFieldElement _2_10_0 = default; Multiply(ref _2_10_0, in t, in _2_5_0);
        
        Square(ref t, in _2_10_0);
        for (int i = 0; i < 9; i++) Square(ref t, in t);
        BatchedFieldElement _2_20_0 = default; Multiply(ref _2_20_0, in t, in _2_10_0);
        
        Square(ref t, in _2_20_0);
        for (int i = 0; i < 19; i++) Square(ref t, in t);
        BatchedFieldElement _2_40_0 = default; Multiply(ref _2_40_0, in t, in _2_20_0);
        
        Square(ref t, in _2_40_0);
        for (int i = 0; i < 9; i++) Square(ref t, in t);
        BatchedFieldElement _2_50_0 = default; Multiply(ref _2_50_0, in t, in _2_10_0);
        
        Square(ref t, in _2_50_0);
        for (int i = 0; i < 49; i++) Square(ref t, in t);
        BatchedFieldElement _2_100_0 = default; Multiply(ref _2_100_0, in t, in _2_50_0);
        
        Square(ref t, in _2_100_0);
        for (int i = 0; i < 99; i++) Square(ref t, in t);
        BatchedFieldElement _2_200_0 = default; Multiply(ref _2_200_0, in t, in _2_100_0);
        
        Square(ref t, in _2_200_0);
        for (int i = 0; i < 49; i++) Square(ref t, in t);
        BatchedFieldElement _2_250_0 = default; Multiply(ref _2_250_0, in t, in _2_50_0);
        
        Square(ref t, in _2_250_0);
        for (int i = 0; i < 4; i++) Square(ref t, in t);
        Multiply(ref r, in t, in _11);
    }
    
    public static BatchedFieldElement FromBytes4(
        ReadOnlySpan<byte> d0, ReadOnlySpan<byte> d1, ReadOnlySpan<byte> d2, ReadOnlySpan<byte> d3)
    {
        FieldElement fe0 = FieldElement.FromBytes(d0);
        FieldElement fe1 = FieldElement.FromBytes(d1);
        FieldElement fe2 = FieldElement.FromBytes(d2);
        FieldElement fe3 = FieldElement.FromBytes(d3);
        
        return new BatchedFieldElement(
            Vector256.Create(fe0.L0, fe1.L0, fe2.L0, fe3.L0),
            Vector256.Create(fe0.L1, fe1.L1, fe2.L1, fe3.L1),
            Vector256.Create(fe0.L2, fe1.L2, fe2.L2, fe3.L2),
            Vector256.Create(fe0.L3, fe1.L3, fe2.L3, fe3.L3),
            Vector256.Create(fe0.L4, fe1.L4, fe2.L4, fe3.L4)
        );
    }
    
    public void ToBytes4(Span<byte> o0, Span<byte> o1, Span<byte> o2, Span<byte> o3)
    {
        FieldElement fe0 = new FieldElement(L0.GetElement(0), L1.GetElement(0), L2.GetElement(0), L3.GetElement(0), L4.GetElement(0));
        FieldElement fe1 = new FieldElement(L0.GetElement(1), L1.GetElement(1), L2.GetElement(1), L3.GetElement(1), L4.GetElement(1));
        FieldElement fe2 = new FieldElement(L0.GetElement(2), L1.GetElement(2), L2.GetElement(2), L3.GetElement(2), L4.GetElement(2));
        FieldElement fe3 = new FieldElement(L0.GetElement(3), L1.GetElement(3), L2.GetElement(3), L3.GetElement(3), L4.GetElement(3));
        
        fe0.ToBytes(o0);
        fe1.ToBytes(o1);
        fe2.ToBytes(o2);
        fe3.ToBytes(o3);
    }
}