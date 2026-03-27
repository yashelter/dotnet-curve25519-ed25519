using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Core.Base;

using static System.Math;

// Используем Explicit Layout
// 5 limbs * 64 bit = 320 bit total (полезных 255)
[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct FieldElement(ulong l0, ulong l1, ulong l2, ulong l3, ulong l4)
{
    [FieldOffset(0)] internal ulong L0 = l0;
    [FieldOffset(8)] internal ulong L1 = l1;
    [FieldOffset(16)] internal ulong L2 = l2;
    [FieldOffset(24)] internal ulong L3 = l3;
    [FieldOffset(32)] internal ulong L4 = l4;
    
    private const ulong MASK51 = (1UL << 51) - 1;
    
    // Константы
    public static readonly FieldElement Zero = new FieldElement(0, 0, 0, 0, 0);
    public static readonly FieldElement One = new FieldElement(1, 0, 0, 0, 0);
    public static readonly FieldElement A24 = new FieldElement(121665, 0, 0, 0, 0);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Mac(ref ulong hi, ref ulong lo, ulong a, ulong b)
    {
        ulong h = BigMul(a, b, out ulong l);
        lo += l;
        hi += h + (lo < l ? 1UL : 0UL);
    }
    
    
    public static void Add(ref FieldElement result, in FieldElement a, in FieldElement b)
    {
        result.L0 = a.L0 + b.L0;
        result.L1 = a.L1 + b.L1;
        result.L2 = a.L2 + b.L2;
        result.L3 = a.L3 + b.L3;
        result.L4 = a.L4 + b.L4;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Sub(ref FieldElement result, in FieldElement a, in FieldElement b)
    {
        result.L0 = (a.L0 + 0xFFFFFFFFFFFDAUL) - b.L0;
        result.L1 = (a.L1 + 0xFFFFFFFFFFFFEUL) - b.L1;
        result.L2 = (a.L2 + 0xFFFFFFFFFFFFEUL) - b.L2;
        result.L3 = (a.L3 + 0xFFFFFFFFFFFFEUL) - b.L3;
        result.L4 = (a.L4 + 0xFFFFFFFFFFFFEUL) - b.L4;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apm(ref FieldElement resAdd, ref FieldElement resSub, in FieldElement a, in FieldElement b)
    {
        ulong a0 = a.L0, a1 = a.L1, a2 = a.L2, a3 = a.L3, a4 = a.L4;
        ulong b0 = b.L0, b1 = b.L1, b2 = b.L2, b3 = b.L3, b4 = b.L4;
        
        resAdd.L0 = a0 + b0;
        resAdd.L1 = a1 + b1;
        resAdd.L2 = a2 + b2;
        resAdd.L3 = a3 + b3;
        resAdd.L4 = a4 + b4;
        
        resSub.L0 = (a0 + 0xFFFFFFFFFFFDAUL) - b0;
        resSub.L1 = (a1 + 0xFFFFFFFFFFFFEUL) - b1;
        resSub.L2 = (a2 + 0xFFFFFFFFFFFFEUL) - b2;
        resSub.L3 = (a3 + 0xFFFFFFFFFFFFEUL) - b3;
        resSub.L4 = (a4 + 0xFFFFFFFFFFFFEUL) - b4;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Multiply(ref FieldElement result, in FieldElement f, in FieldElement g)
    {
        ulong f0 = f.L0, f1 = f.L1, f2 = f.L2, f3 = f.L3, f4 = f.L4;
        ulong g0 = g.L0, g1 = g.L1, g2 = g.L2, g3 = g.L3, g4 = g.L4;
        
        ulong hi, lo, h, l, c;

        // --- Вычисляем r0 ---
        hi = BigMul(f0, g0, out lo);
        h = BigMul(f1, g4 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, g3 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, g2 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, g1 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res0 = lo & MASK51; // Сохраняем младшие 51 бит
        c = (lo >> 51) | (hi << 13); // Весь остальной 77-битный хвост переносим дальше
        
        // --- Вычисляем r1 (переиспользуем регистры hi, lo, h, l) ---
        hi = BigMul(f0, g1, out lo); lo += c; hi += (lo < c ? 1UL : 0UL); // Добавляем перенос
        h = BigMul(f1, g0, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, g4 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, g3 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, g2 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res1 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- Вычисляем r2 ---
        hi = BigMul(f0, g2, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1, g1, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, g0, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, g4 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, g3 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res2 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- Вычисляем r3 ---
        hi = BigMul(f0, g3, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1, g2, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, g1, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, g0, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, g4 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res3 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- Вычисляем r4 ---
        hi = BigMul(f0, g4, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1, g3, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, g2, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, g1, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, g0, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res4 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- Финальная редукция ---
        res0 += c * 19;
        c = res0 >> 51;
        
        result.L0 = res0 & MASK51;
        result.L1 = res1 + c;
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Square(ref FieldElement result, in FieldElement f)
    {
        ulong f0 = f.L0, f1 = f.L1, f2 = f.L2, f3 = f.L3, f4 = f.L4;
        
        ulong hi, lo, h, l, c;

        // --- r0 ---
        hi = BigMul(f0, f0, out lo);
        h = BigMul(f1, f4 * 38, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, f3 * 38, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res0 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- r1 ---
        hi = BigMul(f0 * 2, f1, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f2, f4 * 38, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, f3 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res1 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- r2 ---
        hi = BigMul(f0 * 2, f2, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1, f1, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f3, f4 * 38, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res2 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- r3 ---
        hi = BigMul(f0 * 2, f3, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1 * 2, f2, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f4, f4 * 19, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res3 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- r4 ---
        hi = BigMul(f0 * 2, f4, out lo); lo += c; hi += (lo < c ? 1UL : 0UL);
        h = BigMul(f1 * 2, f3, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        h = BigMul(f2, f2, out l); lo += l; hi += h + (lo < l ? 1UL : 0UL);
        
        ulong res4 = lo & MASK51;
        c = (lo >> 51) | (hi << 13);
        
        // --- Финальная редукция ---
        res0 += c * 19;
        c = res0 >> 51;
        
        result.L0 = res0 & MASK51;
        result.L1 = res1 + c;
        result.L2 = res2;
        result.L3 = res3;
        result.L4 = res4;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CSwap(ref FieldElement a, ref FieldElement b, int swap)
    {
        ulong mask = (ulong)-swap;
        ulong t0 = mask & (a.L0 ^ b.L0);
        ulong t1 = mask & (a.L1 ^ b.L1);
        ulong t2 = mask & (a.L2 ^ b.L2);
        ulong t3 = mask & (a.L3 ^ b.L3);
        ulong t4 = mask & (a.L4 ^ b.L4);
        
        a.L0 ^= t0; b.L0 ^= t0;
        a.L1 ^= t1; b.L1 ^= t1;
        a.L2 ^= t2; b.L2 ^= t2;
        a.L3 ^= t3; b.L3 ^= t3;
        a.L4 ^= t4; b.L4 ^= t4;
    }
    

    // <summary>
    /// Вычисляет обратный элемент (z^(-1)) по модулю 2^255-19.
    /// Использует Малую теорему Ферма: z^(p-2).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static FieldElement Invert(in FieldElement z)
    {
        FieldElement _2 = default, _4 = default, _8 = default, _9 = default;
        FieldElement _11 = default, _22 = default, _2_5_0 = default;
        
        Square(ref _2, in z); // 2
        Square(ref _4, in _2); // 4
        Square(ref _8, in _4); // 8
        Multiply(ref _9, in _8, in z); // 9
        Multiply(ref _11, in _9, in _2); // 11
        Square(ref _22, in _11); // 22
        Multiply(ref _2_5_0, in _22, in _9); // 31 (2^5 - 2^0)
        
        FieldElement _2_10_0 = default;
        Square(ref _2_10_0, in _2_5_0);
        for (int i = 0; i < 4; i++) Square(ref _2_10_0, in _2_10_0);
        Multiply(ref _2_10_0, in _2_10_0, in _2_5_0);
        
        FieldElement _2_20_0 = default;
        Square(ref _2_20_0, in _2_10_0);
        for (int i = 0; i < 9; i++) Square(ref _2_20_0, in _2_20_0);
        Multiply(ref _2_20_0, in _2_20_0, in _2_10_0);
        
        FieldElement _2_40_0 = default;
        Square(ref _2_40_0, in _2_20_0);
        for (int i = 0; i < 19; i++) Square(ref _2_40_0, in _2_40_0);
        Multiply(ref _2_40_0, in _2_40_0, in _2_20_0);
        
        FieldElement _2_50_0 = default;
        Square(ref _2_50_0, in _2_40_0);
        for (int i = 0; i < 9; i++) Square(ref _2_50_0, in _2_50_0);
        Multiply(ref _2_50_0, in _2_50_0, in _2_10_0);
        
        FieldElement _2_100_0 = default;
        Square(ref _2_100_0, in _2_50_0);
        for (int i = 0; i < 49; i++) Square(ref _2_100_0, in _2_100_0);
        Multiply(ref _2_100_0, in _2_100_0, in _2_50_0);
        
        FieldElement _2_200_0 = default;
        Square(ref _2_200_0, in _2_100_0);
        for (int i = 0; i < 99; i++) Square(ref _2_200_0, in _2_200_0);
        Multiply(ref _2_200_0, in _2_200_0, in _2_100_0);
        
        FieldElement _2_250_0 = default;
        Square(ref _2_250_0, in _2_200_0);
        for (int i = 0; i < 49; i++) Square(ref _2_250_0, in _2_250_0);
        Multiply(ref _2_250_0, in _2_250_0, in _2_50_0);
        
        FieldElement _2_255_21 = default;
        Square(ref _2_255_21, in _2_250_0);
        for (int i = 0; i < 4; i++) Square(ref _2_255_21, in _2_255_21);
        
        FieldElement result = default;
        Multiply(ref result, in _2_255_21, in _11); // умножаем на z^11
        
        return result;
    }
    
    /// <summary>
    /// Парсит 32 байта (Little-Endian) в элемент поля Radix-51.
    /// Согласно RFC, игнорирует старший бит последнего байта для X25519.
    /// </summary>
    public static FieldElement FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 32) throw new ArgumentException("Input must be 32 bytes.");
        
        // Парсим байты в 4 64-битных числа
        ulong t0 = BitConverter.ToUInt64(data.Slice(0, 8));
        ulong t1 = BitConverter.ToUInt64(data.Slice(8, 8));
        ulong t2 = BitConverter.ToUInt64(data.Slice(16, 8));
        ulong t3 = BitConverter.ToUInt64(data.Slice(24, 8));
        
        t3 &= 0x7FFFFFFFFFFFFFFF; // Маскируем старший бит (RFC 7748: "MUST mask the most significant bit")
        
        // Перепаковываем из 64-битных блоков в 51-битные лимбы
        ulong l0 = t0 & MASK51;
        ulong l1 = ((t0 >> 51) | (t1 << 13)) & MASK51;
        ulong l2 = ((t1 >> 38) | (t2 << 26)) & MASK51;
        ulong l3 = ((t2 >> 25) | (t3 << 39)) & MASK51;
        ulong l4 = (t3 >> 12) & MASK51;
        
        return new FieldElement(l0, l1, l2, l3, l4);
    }
    
    /// <summary>
    /// Строгая редукция по модулю p и конвертация в 32 байта.
    /// </summary>
    public void ToBytes(Span<byte> output)
    {
        if (output.Length < 32) throw new ArgumentException("Output must be at least 32 bytes.");
        
        // 1. Полностью проталкиваем переносы (слабая редукция)
        ulong h0 = L0, h1 = L1, h2 = L2, h3 = L3, h4 = L4;
        ulong c;
        
        c = h0 >> 51; h0 &= MASK51; h1 += c;
        c = h1 >> 51; h1 &= MASK51; h2 += c;
        c = h2 >> 51; h2 &= MASK51; h3 += c;
        c = h3 >> 51; h3 &= MASK51; h4 += c; 
        c = h4 >> 51; h4 &= MASK51; h0 += c * 19;
        
        c = h0 >> 51; h0 &= MASK51; h1 += c;
        c = h1 >> 51; h1 &= MASK51; h2 += c;
        c = h2 >> 51; h2 &= MASK51; h3 += c;
        c = h3 >> 51; h3 &= MASK51; h4 += c;
        
        // Теперь значение h гарантированно меньше 2p, но может быть >= p.
        // Вычислим h_minus_p = h - p = h - (2^255 - 19) = h - 2^255 + 19.
        ulong s0 = h0 + 19; c = s0 >> 51; s0 &= MASK51;
        ulong s1 = h1 + c; c = s1 >> 51; s1 &= MASK51;
        ulong s2 = h2 + c; c = s2 >> 51; s2 &= MASK51;
        ulong s3 = h3 + c; c = s3 >> 51; s3 &= MASK51; 
        ulong s4 = h4 + c;
        
        // Значение p имеет 1 в 255-м бите (это 50-й бит в L4).
        // Если h >= p, то при добавлении 19 к h 50-й бит L4 перейдет в 51-й.
        // То есть s4 >> 51 будет 1. Это наша маска "число было больше или равно p".
        ulong mask = (ulong)-(long)(s4 >> 51);
        s4 &= MASK51;
        
        // Если mask = 0xFF..FF (h >= p), берем s (оно же h-p). Иначе берем h.
        ulong r0 = (h0 & ~mask) | (s0 & mask);
        ulong r1 = (h1 & ~mask) | (s1 & mask);
        ulong r2 = (h2 & ~mask) | (s2 & mask);
        ulong r3 = (h3 & ~mask) | (s3 & mask);
        ulong r4 = (h4 & ~mask) | (s4 & mask);
        
        // Перепаковываем 51-битные лимбы в 64-битные
        ulong t0 = r0 | (r1 << 51);
        ulong t1 = (r1 >> 13) | (r2 << 38);
        ulong t2 = (r2 >> 26) | (r3 << 25);
        ulong t3 = (r3 >> 39) | (r4 << 12);
        
        // Записываем в байты
        BitConverter.TryWriteBytes(output.Slice(0, 8), t0);
        BitConverter.TryWriteBytes(output.Slice(8, 8), t1);
        BitConverter.TryWriteBytes(output.Slice(16, 8), t2);
        BitConverter.TryWriteBytes(output.Slice(24, 8), t3);
    }
}

