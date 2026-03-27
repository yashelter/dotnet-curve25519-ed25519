using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Core.Base;


[StructLayout(LayoutKind.Sequential)]
#if UNSAFE_ENABLED
[System.Runtime.CompilerServices.SkipLocalsInit]
#endif
public struct FieldElement4(
    Vector256<ulong> l0,
    Vector256<ulong> l1,
    Vector256<ulong> l2,
    Vector256<ulong> l3,
    Vector256<ulong> l4)
{
    internal Vector256<ulong> L0 = l0, L1 = l1, L2 = l2, L3 = l3, L4 = l4;
    
    private static readonly Vector256<ulong> Mask51Vec = Vector256.Create((1UL << 51) - 1);
    private static readonly Vector256<ulong> AddConst0 = Vector256.Create(0xFFFFFFFFFFFDAUL);
    private static readonly Vector256<ulong> AddConst1 = Vector256.Create(0xFFFFFFFFFFFFEUL);
    
    public static readonly FieldElement4 Zero = new(
        Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly FieldElement4 One = new(
        Vector256.Create(1UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly FieldElement4 A24 = new(
        Vector256.Create(121665UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    // --- Векторные сложения, вычитания и обмен ---
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Add(ref FieldElement4 result, in FieldElement4 a, in FieldElement4 b)
    {
        result.L0 = Avx2.Add(a.L0, b.L0);
        result.L1 = Avx2.Add(a.L1, b.L1);
        result.L2 = Avx2.Add(a.L2, b.L2);
        result.L3 = Avx2.Add(a.L3, b.L3);
        result.L4 = Avx2.Add(a.L4, b.L4);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Sub(ref FieldElement4 result, in FieldElement4 a, in FieldElement4 b)
    {
        result.L0 = Avx2.Subtract(Avx2.Add(a.L0, AddConst0), b.L0);
        result.L1 = Avx2.Subtract(Avx2.Add(a.L1, AddConst1), b.L1);
        result.L2 = Avx2.Subtract(Avx2.Add(a.L2, AddConst1), b.L2);
        result.L3 = Avx2.Subtract(Avx2.Add(a.L3, AddConst1), b.L3);
        result.L4 = Avx2.Subtract(Avx2.Add(a.L4, AddConst1), b.L4);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apm(ref FieldElement4 resAdd, ref FieldElement4 resSub, in FieldElement4 a, in FieldElement4 b)
    {
        resAdd.L0 = Avx2.Add(a.L0, b.L0);
        resAdd.L1 = Avx2.Add(a.L1, b.L1);
        resAdd.L2 = Avx2.Add(a.L2, b.L2);
        resAdd.L3 = Avx2.Add(a.L3, b.L3);
        resAdd.L4 = Avx2.Add(a.L4, b.L4);
        
        resSub.L0 = Avx2.Subtract(Avx2.Add(a.L0, AddConst0), b.L0);
        resSub.L1 = Avx2.Subtract(Avx2.Add(a.L1, AddConst1), b.L1);
        resSub.L2 = Avx2.Subtract(Avx2.Add(a.L2, AddConst1), b.L2);
        resSub.L3 = Avx2.Subtract(Avx2.Add(a.L3, AddConst1), b.L3);
        resSub.L4 = Avx2.Subtract(Avx2.Add(a.L4, AddConst1), b.L4);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CSwap(ref FieldElement4 a, ref FieldElement4 b, Vector256<ulong> swapBits)
    {
        // Маска: 0x00...00 или 0xFF...FF
        var mask = Avx2.Subtract(Vector256<ulong>.Zero, swapBits);
        
        var t0 = Avx2.And(mask, Avx2.Xor(a.L0, b.L0));
        a.L0 = Avx2.Xor(a.L0, t0); b.L0 = Avx2.Xor(b.L0, t0);
        
        var t1 = Avx2.And(mask, Avx2.Xor(a.L1, b.L1));
        a.L1 = Avx2.Xor(a.L1, t1); b.L1 = Avx2.Xor(b.L1, t1);
        
        var t2 = Avx2.And(mask, Avx2.Xor(a.L2, b.L2));
        a.L2 = Avx2.Xor(a.L2, t2); b.L2 = Avx2.Xor(b.L2, t2);
        
        var t3 = Avx2.And(mask, Avx2.Xor(a.L3, b.L3));
        a.L3 = Avx2.Xor(a.L3, t3); b.L3 = Avx2.Xor(b.L3, t3);
        
        var t4 = Avx2.And(mask, Avx2.Xor(a.L4, b.L4));
        a.L4 = Avx2.Xor(a.L4, t4); b.L4 = Avx2.Xor(b.L4, t4);
    }
    
    
    // --- Вспомогательная логика для векторизованного BigMul ---
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<ulong> CarryMask(Vector256<ulong> a, Vector256<ulong> sum)
    {
        // Вычисляет unsigned(A) > unsigned(Sum). Если да, значит было переполнение (Carry = 1).
        Vector256<ulong> signBit = Vector256.Create(0x8000000000000000UL);
        Vector256<long> a_cmp = Avx2.Xor(a, signBit).AsInt64();
        Vector256<long> sum_cmp = Avx2.Xor(sum, signBit).AsInt64();
        return Vector256.AsUInt64(Avx2.CompareGreaterThan(a_cmp, sum_cmp)); // Возвращает 0xFF...FF при переносе
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    
    private static void BigMul(Vector256<ulong> a, Vector256<ulong> b, out Vector256<ulong> low, out Vector256<ulong> high)
    {
        Vector256<uint> a32 = Vector256.AsUInt32(a);
        Vector256<uint> b32 = Vector256.AsUInt32(b);
        Vector256<uint> a_shr = Vector256.AsUInt32(Avx2.ShiftRightLogical(a, 32));
        Vector256<uint> b_shr = Vector256.AsUInt32(Avx2.ShiftRightLogical(b, 32));
        
        Vector256<ulong> a0b0 = Avx2.Multiply(a32, b32);
        Vector256<ulong> a1b1 = Avx2.Multiply(a_shr, b_shr);
        Vector256<ulong> a0b1 = Avx2.Multiply(a32, b_shr);
        Vector256<ulong> a1b0 = Avx2.Multiply(a_shr, b32);
        
        Vector256<ulong> mid = Avx2.Add(a0b1, a1b0); // max ~53 бита, не переполняет 64 бита
        Vector256<ulong> mid_lo = Avx2.ShiftLeftLogical(mid, 32);
        Vector256<ulong> mid_hi = Avx2.ShiftRightLogical(mid, 32);
        
        low = Avx2.Add(a0b0, mid_lo);
        Vector256<ulong> carry = CarryMask(a0b0, low);
        
        // carry == 0xFF...FF, поэтому вычитание работает как прибавление 1.
        high = Avx2.Subtract(Avx2.Add(a1b1, mid_hi), carry);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void AddCarry(ref Vector256<ulong> hi, ref Vector256<ulong> lo, Vector256<ulong> c)
    {
        Vector256<ulong> new_lo = Avx2.Add(lo, c);
        Vector256<ulong> carry = CarryMask(lo, new_lo);
        lo = new_lo;
        hi = Avx2.Subtract(hi, carry);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Mac(ref Vector256<ulong> hi, ref Vector256<ulong> lo, Vector256<ulong> a, Vector256<ulong> b)
    {
        BigMul(a, b, out Vector256<ulong> l, out Vector256<ulong> h);
        AddCarry(ref hi, ref lo, l);
        hi = Avx2.Add(hi, h);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<ulong> Mul19(Vector256<ulong> x)
    {
        Vector256<ulong> x2 = Avx2.ShiftLeftLogical(x, 1);
        Vector256<ulong> x16 = Avx2.ShiftLeftLogical(x, 4);
        return Avx2.Add(Avx2.Add(x16, x2), x);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<ulong> Mul38(Vector256<ulong> x)
    {
        Vector256<ulong> x2 = Avx2.ShiftLeftLogical(x, 1);
        Vector256<ulong> x4 = Avx2.ShiftLeftLogical(x, 2);
        Vector256<ulong> x32 = Avx2.ShiftLeftLogical(x, 5);
        return Avx2.Add(Avx2.Add(x32, x4), x2);
    }
    
    
    // --- Полностью Векторизованные Square и Multiply ---
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Multiply(ref FieldElement4 result, in FieldElement4 f, in FieldElement4 g)
    {
        Vector256<ulong> f0 = f.L0; Vector256<ulong> f1 = f.L1;
        Vector256<ulong> f2 = f.L2; Vector256<ulong> f3 = f.L3;
        Vector256<ulong> f4 = f.L4; Vector256<ulong> g0 = g.L0;
        Vector256<ulong> g1 = g.L1; Vector256<ulong> g2 = g.L2;
        Vector256<ulong> g3 = g.L3; Vector256<ulong> g4 = g.L4;
        
        Vector256<ulong> g1_19 = Mul19(g1);
        Vector256<ulong> g2_19 = Mul19(g2);
        Vector256<ulong> g3_19 = Mul19(g3);
        Vector256<ulong> g4_19 = Mul19(g4);
        Vector256<ulong> hi, lo, c;
        
        // r0
        BigMul(f0, g0, out lo, out hi);
        Mac(ref hi, ref lo, f1, g4_19); Mac(ref hi, ref lo, f2, g3_19);
        Mac(ref hi, ref lo, f3, g2_19); Mac(ref hi, ref lo, f4, g1_19);
        Vector256<ulong> res0 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r1
        BigMul(f0, g1, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g0); Mac(ref hi, ref lo, f2, g4_19);
        Mac(ref hi, ref lo, f3, g3_19); Mac(ref hi, ref lo, f4, g2_19);
        Vector256<ulong> res1 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r2
        BigMul(f0, g2, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g1); Mac(ref hi, ref lo, f2, g0);
        Mac(ref hi, ref lo, f3, g4_19); Mac(ref hi, ref lo, f4, g3_19);
        Vector256<ulong> res2 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r3
        BigMul(f0, g3, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g2); Mac(ref hi, ref lo, f2, g1);
        Mac(ref hi, ref lo, f3, g0); Mac(ref hi, ref lo, f4, g4_19);
        Vector256<ulong> res3 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r4
        BigMul(f0, g4, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, g3); Mac(ref hi, ref lo, f2, g2);
        Mac(ref hi, ref lo, f3, g1); Mac(ref hi, ref lo, f4, g0);
        Vector256<ulong> res4 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // Финальная редукция
        res0 = Avx2.Add(res0, Mul19(c));
        c = Avx2.ShiftRightLogical(res0, 51);
        
        result.L0 = Avx2.And(res0, Mask51Vec);
        result.L1 = Avx2.Add(res1, c);
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Square(ref FieldElement4 result, in FieldElement4 f)
    {
        Vector256<ulong> f0 = f.L0;
        Vector256<ulong> f1 = f.L1;
        Vector256<ulong> f2 = f.L2;
        Vector256<ulong> f3 = f.L3;
        Vector256<ulong> f4 = f.L4;
        
        Vector256<ulong> f0_2 = Avx2.ShiftLeftLogical(f0, 1);
        Vector256<ulong> f1_2 = Avx2.ShiftLeftLogical(f1, 1);
        
        Vector256<ulong> f3_19 = Mul19(f3);
        Vector256<ulong> f4_19 = Mul19(f4);
        Vector256<ulong> f3_38 = Mul38(f3);
        Vector256<ulong> f4_38 = Mul38(f4);
        Vector256<ulong> hi, lo, c;
        
        // r0
        BigMul(f0, f0, out lo, out hi);
        Mac(ref hi, ref lo, f1, f4_38); Mac(ref hi, ref lo, f2, f3_38);
        Vector256<ulong> res0 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r1
        BigMul(f0_2, f1, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f2, f4_38); Mac(ref hi, ref lo, f3, f3_19);
        Vector256<ulong> res1 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r2
        BigMul(f0_2, f2, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1, f1); Mac(ref hi, ref lo, f3, f4_38);
        Vector256<ulong> res2 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r3
        BigMul(f0_2, f3, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1_2, f2); Mac(ref hi, ref lo, f4, f4_19);
        Vector256<ulong> res3 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // r4
        BigMul(f0_2, f4, out lo, out hi); AddCarry(ref hi, ref lo, c);
        Mac(ref hi, ref lo, f1_2, f3); Mac(ref hi, ref lo, f2, f2);
        Vector256<ulong> res4 = Avx2.And(lo, Mask51Vec);
        c = Avx2.Or(Avx2.ShiftRightLogical(lo, 51), Avx2.ShiftLeftLogical(hi, 13));
        
        // Финальная редукция
        res0 = Avx2.Add(res0, Mul19(c));
        c = Avx2.ShiftRightLogical(res0, 51);
        
        result.L0 = Avx2.And(res0, Mask51Vec);
        result.L1 = Avx2.Add(res1, c);
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
    
    // --- Распаковка/Упаковка ---
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static FieldElement4 FromFields(in FieldElement e0, in FieldElement e1, in FieldElement e2, in FieldElement e3)
    {
        return new FieldElement4(
            Vector256.Create(e0.L0, e1.L0, e2.L0, e3.L0), Vector256.Create(e0.L1, e1.L1, e2.L1, e3.L1),
            Vector256.Create(e0.L2, e1.L2, e2.L2, e3.L2), Vector256.Create(e0.L3, e1.L3, e2.L3, e3.L3),
            Vector256.Create(e0.L4, e1.L4, e2.L4, e3.L4));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static FieldElement4 FromBytes(ReadOnlySpan<byte> data) => FromFields(
        FieldElement.FromBytes(data.Slice(0, 32)), FieldElement.FromBytes(data.Slice(32, 32)),
        FieldElement.FromBytes(data.Slice(64, 32)), FieldElement.FromBytes(data.Slice(96, 32)));
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void ToBytes(Span<byte> output)
    {
        new FieldElement(L0.GetElement(0), L1.GetElement(0), L2.GetElement(0), L3.GetElement(0), L4.GetElement(0)).ToBytes(output.Slice(0, 32));
        new FieldElement(L0.GetElement(1), L1.GetElement(1), L2.GetElement(1), L3.GetElement(1), L4.GetElement(1)).ToBytes(output.Slice(32, 32));
        new FieldElement(L0.GetElement(2), L1.GetElement(2), L2.GetElement(2), L3.GetElement(2), L4.GetElement(2)).ToBytes(output.Slice(64, 32));
        new FieldElement(L0.GetElement(3), L1.GetElement(3), L2.GetElement(3), L3.GetElement(3), L4.GetElement(3)).ToBytes(output.Slice(96, 32));
    }
}