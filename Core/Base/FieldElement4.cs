using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Core.Base;


[StructLayout(LayoutKind.Sequential)]
public struct FieldElement4
{
    internal Vector256<ulong> L0, L1, L2, L3, L4;

    private static readonly Vector256<ulong> Mask51Vec = Vector256.Create((1UL << 51) - 1);
    private static readonly Vector256<ulong> AddConst0 = Vector256.Create(0xFFFFFFFFFFFDAUL);
    private static readonly Vector256<ulong> AddConst1 = Vector256.Create(0xFFFFFFFFFFFFEUL);

    public static readonly FieldElement4 Zero = new(
        Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);

    public static readonly FieldElement4 One = new(
        Vector256.Create(1UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);

    public static readonly FieldElement4 A24 = new(
        Vector256.Create(121665UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FieldElement4(Vector256<ulong> l0, Vector256<ulong> l1, Vector256<ulong> l2, Vector256<ulong> l3, Vector256<ulong> l4)
    {
        L0 = l0; L1 = l1; L2 = l2; L3 = l3; L4 = l4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(ref FieldElement4 result, in FieldElement4 a, in FieldElement4 b)
    {
        result.L0 = a.L0 + b.L0;
        result.L1 = a.L1 + b.L1;
        result.L2 = a.L2 + b.L2;
        result.L3 = a.L3 + b.L3;
        result.L4 = a.L4 + b.L4;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sub(ref FieldElement4 result, in FieldElement4 a, in FieldElement4 b)
    {
        result.L0 = (a.L0 + AddConst0) - b.L0;
        result.L1 = (a.L1 + AddConst1) - b.L1;
        result.L2 = (a.L2 + AddConst1) - b.L2;
        result.L3 = (a.L3 + AddConst1) - b.L3;
        result.L4 = (a.L4 + AddConst1) - b.L4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Apm(ref FieldElement4 resAdd, ref FieldElement4 resSub, in FieldElement4 a, in FieldElement4 b)
    {
        resAdd.L0 = a.L0 + b.L0;
        resAdd.L1 = a.L1 + b.L1;
        resAdd.L2 = a.L2 + b.L2;
        resAdd.L3 = a.L3 + b.L3;
        resAdd.L4 = a.L4 + b.L4;

        resSub.L0 = (a.L0 + AddConst0) - b.L0;
        resSub.L1 = (a.L1 + AddConst1) - b.L1;
        resSub.L2 = (a.L2 + AddConst1) - b.L2;
        resSub.L3 = (a.L3 + AddConst1) - b.L3;
        resSub.L4 = (a.L4 + AddConst1) - b.L4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CSwap(ref FieldElement4 a, ref FieldElement4 b, Vector256<ulong> swapBits)
    {
        var mask = Vector256<ulong>.Zero - swapBits; // 0 -> 0x00..00, 1 -> 0xFF..FF

        var t0 = mask & (a.L0 ^ b.L0);
        a.L0 ^= t0; b.L0 ^= t0;

        var t1 = mask & (a.L1 ^ b.L1);
        a.L1 ^= t1; b.L1 ^= t1;

        var t2 = mask & (a.L2 ^ b.L2);
        a.L2 ^= t2; b.L2 ^= t2;

        var t3 = mask & (a.L3 ^ b.L3);
        a.L3 ^= t3; b.L3 ^= t3;

        var t4 = mask & (a.L4 ^ b.L4);
        a.L4 ^= t4; b.L4 ^= t4;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BigMul(Vector256<ulong> a, Vector256<ulong> b, out Vector256<ulong> low, out Vector256<ulong> high)
    {
        var a32 = a.AsUInt32();
        var b32 = b.AsUInt32();
        var a_shr = (a >>> 32).AsUInt32();
        var b_shr = (b >>> 32).AsUInt32();

        // Мы оставляем Avx2.Multiply, так как он гарантированно использует pmuludq (нижние 32 на 32 в 64)
        var a0b0 = Avx2.Multiply(a32, b32);
        var a1b1 = Avx2.Multiply(a_shr, b_shr);
        var a0b1 = Avx2.Multiply(a32, b_shr);
        var a1b0 = Avx2.Multiply(a_shr, b32);

        var mid = a0b1 + a1b0; 
        var mid_lo = mid << 32;
        var mid_hi = mid >>> 32;

        low = a0b0 + mid_lo;
        var carry = Vector256.GreaterThan(a0b0, low); 
        
        // carry генерирует вектор из 0xFF...FF (-1). Вычитание -1 равноценно прибавлению 1!
        high = a1b1 + mid_hi - carry;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddCarry(ref Vector256<ulong> hi, ref Vector256<ulong> lo, Vector256<ulong> c)
    {
        var new_lo = lo + c;
        var carry = Vector256.GreaterThan(lo, new_lo);
        lo = new_lo;
        hi = hi - carry;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Mac(ref Vector256<ulong> hi, ref Vector256<ulong> lo, Vector256<ulong> a, Vector256<ulong> b)
    {
        BigMul(a, b, out var l, out var h);
        AddCarry(ref hi, ref lo, l);
        hi += h;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Mul19(Vector256<ulong> x) => (x << 4) + (x << 1) + x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Mul38(Vector256<ulong> x) => (x << 5) + (x << 2) + (x << 1);
    
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(ref FieldElement4 result, in FieldElement4 f, in FieldElement4 g)
    {
        var f0 = f.L0; var f1 = f.L1; var f2 = f.L2; var f3 = f.L3; var f4 = f.L4;
        var g0 = g.L0; var g1 = g.L1; var g2 = g.L2; var g3 = g.L3; var g4 = g.L4;
        
        var g1_19 = Mul19(g1); var g2_19 = Mul19(g2); var g3_19 = Mul19(g3); var g4_19 = Mul19(g4);
        Vector256<ulong> hi, lo, c;

        // r0
        BigMul(f0, g0, out lo, out hi);
        Mac(ref hi, ref lo, f1, g4_19); Mac(ref hi, ref lo, f2, g3_19);
        Mac(ref hi, ref lo, f3, g2_19); Mac(ref hi, ref lo, f4, g1_19);
        var res0 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r1
        BigMul(f0, g1, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g0); Mac(ref hi, ref lo, f2, g4_19);
        Mac(ref hi, ref lo, f3, g3_19); Mac(ref hi, ref lo, f4, g2_19);
        var res1 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r2
        BigMul(f0, g2, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g1); Mac(ref hi, ref lo, f2, g0);
        Mac(ref hi, ref lo, f3, g4_19); Mac(ref hi, ref lo, f4, g3_19);
        var res2 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r3
        BigMul(f0, g3, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g2); Mac(ref hi, ref lo, f2, g1);
        Mac(ref hi, ref lo, f3, g0); Mac(ref hi, ref lo, f4, g4_19);
        var res3 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r4
        BigMul(f0, g4, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g3); Mac(ref hi, ref lo, f2, g2);
        Mac(ref hi, ref lo, f3, g1); Mac(ref hi, ref lo, f4, g0);
        var res4 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // Финальная редукция
        res0 += Mul19(c);
        c = res0 >>> 51;

        result.L0 = res0 & Mask51Vec;
        result.L1 = res1 + c;
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }

   // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Square(ref FieldElement4 result, in FieldElement4 f)
    {
        var f0 = f.L0; var f1 = f.L1; var f2 = f.L2; var f3 = f.L3; var f4 = f.L4;
        var f0_2 = f0 << 1; var f1_2 = f1 << 1;
        
        var f3_19 = Mul19(f3); var f4_19 = Mul19(f4);
        var f3_38 = Mul38(f3); var f4_38 = Mul38(f4);
        Vector256<ulong> hi, lo, c;

        // r0
        BigMul(f0, f0, out lo, out hi);
        Mac(ref hi, ref lo, f1, f4_38); Mac(ref hi, ref lo, f2, f3_38);
        var res0 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r1
        BigMul(f0_2, f1, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f2, f4_38); Mac(ref hi, ref lo, f3, f3_19);
        var res1 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r2
        BigMul(f0_2, f2, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, f1); Mac(ref hi, ref lo, f3, f4_38);
        var res2 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r3
        BigMul(f0_2, f3, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1_2, f2); Mac(ref hi, ref lo, f4, f4_19);
        var res3 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // r4
        BigMul(f0_2, f4, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1_2, f3); Mac(ref hi, ref lo, f2, f2);
        var res4 = lo & Mask51Vec;
        c = (lo >>> 51) | (hi << 13);

        // Финальная редукция
        res0 += Mul19(c);
        c = res0 >>> 51;

        result.L0 = res0 & Mask51Vec;
        result.L1 = res1 + c;
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Invert(ref FieldElement4 result, in FieldElement4 z)
    {
        // Идеально параллельно для 4х элементов сразу
        FieldElement4 _2 = default, _4 = default, _8 = default, _9 = default;
        FieldElement4 _11 = default, _22 = default, _2_5_0 = default;
        
        Square(ref _2, in z); Square(ref _4, in _2); Square(ref _8, in _4);
        Multiply(ref _9, in _8, in z); Multiply(ref _11, in _9, in _2);
        Square(ref _22, in _11); Multiply(ref _2_5_0, in _22, in _9);
        
        FieldElement4 _2_10_0 = default; Square(ref _2_10_0, in _2_5_0);
        for (int i = 0; i < 4; i++) Square(ref _2_10_0, in _2_10_0);
        Multiply(ref _2_10_0, in _2_10_0, in _2_5_0);
        
        FieldElement4 _2_20_0 = default; Square(ref _2_20_0, in _2_10_0);
        for (int i = 0; i < 9; i++) Square(ref _2_20_0, in _2_20_0);
        Multiply(ref _2_20_0, in _2_20_0, in _2_10_0);
        
        FieldElement4 _2_40_0 = default; Square(ref _2_40_0, in _2_20_0);
        for (int i = 0; i < 19; i++) Square(ref _2_40_0, in _2_40_0);
        Multiply(ref _2_40_0, in _2_40_0, in _2_20_0);
        
        FieldElement4 _2_50_0 = default; Square(ref _2_50_0, in _2_40_0);
        for (int i = 0; i < 9; i++) Square(ref _2_50_0, in _2_50_0);
        Multiply(ref _2_50_0, in _2_50_0, in _2_10_0);
        
        FieldElement4 _2_100_0 = default; Square(ref _2_100_0, in _2_50_0);
        for (int i = 0; i < 49; i++) Square(ref _2_100_0, in _2_100_0);
        Multiply(ref _2_100_0, in _2_100_0, in _2_50_0);
        
        FieldElement4 _2_200_0 = default; Square(ref _2_200_0, in _2_100_0);
        for (int i = 0; i < 99; i++) Square(ref _2_200_0, in _2_200_0);
        Multiply(ref _2_200_0, in _2_200_0, in _2_100_0);
        
        FieldElement4 _2_250_0 = default; Square(ref _2_250_0, in _2_200_0);
        for (int i = 0; i < 49; i++) Square(ref _2_250_0, in _2_250_0);
        Multiply(ref _2_250_0, in _2_250_0, in _2_50_0);
        
        FieldElement4 _2_255_21 = default; Square(ref _2_255_21, in _2_250_0);
        for (int i = 0; i < 4; i++) Square(ref _2_255_21, in _2_255_21);
        
        Multiply(ref result, in _2_255_21, in _11);
    }

    // --- Распаковка/Упаковка для IO ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FieldElement4 FromFields(in FieldElement e0, in FieldElement e1, in FieldElement e2, in FieldElement e3)
    {
        return new FieldElement4(
            Vector256.Create(e0.L0, e1.L0, e2.L0, e3.L0), Vector256.Create(e0.L1, e1.L1, e2.L1, e3.L1),
            Vector256.Create(e0.L2, e1.L2, e2.L2, e3.L2), Vector256.Create(e0.L3, e1.L3, e2.L3, e3.L3),
            Vector256.Create(e0.L4, e1.L4, e2.L4, e3.L4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldElement4 FromBytes(ReadOnlySpan<byte> data) => FromFields(
        FieldElement.FromBytes(data.Slice(0, 32)), FieldElement.FromBytes(data.Slice(32, 32)),
        FieldElement.FromBytes(data.Slice(64, 32)), FieldElement.FromBytes(data.Slice(96, 32)));[MethodImpl(MethodImplOptions.AggressiveInlining)]
    
    public readonly void ToBytes(Span<byte> output)
    {
        new FieldElement(L0.GetElement(0), L1.GetElement(0), L2.GetElement(0), L3.GetElement(0), L4.GetElement(0)).ToBytes(output.Slice(0, 32));
        new FieldElement(L0.GetElement(1), L1.GetElement(1), L2.GetElement(1), L3.GetElement(1), L4.GetElement(1)).ToBytes(output.Slice(32, 32));
        new FieldElement(L0.GetElement(2), L1.GetElement(2), L2.GetElement(2), L3.GetElement(2), L4.GetElement(2)).ToBytes(output.Slice(64, 32));
        new FieldElement(L0.GetElement(3), L1.GetElement(3), L2.GetElement(3), L3.GetElement(3), L4.GetElement(3)).ToBytes(output.Slice(96, 32));
    }
}