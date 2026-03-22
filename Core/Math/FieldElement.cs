using System.Runtime.Intrinsics;

namespace Core.Math;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Используем Explicit Layout
// 5 limbs * 64 bit = 320 bit total (полезных 255)
[StructLayout(LayoutKind.Explicit, Size = 40)]
public readonly struct FieldElement(ulong l0, ulong l1, ulong l2, ulong l3, ulong l4)
{
    [FieldOffset(0)] internal readonly ulong L0 = l0;
    [FieldOffset(8)] internal readonly ulong L1 = l1;
    [FieldOffset(16)] internal readonly ulong L2 = l2;
    [FieldOffset(24)] internal readonly ulong L3 = l3;
    [FieldOffset(32)] internal readonly ulong L4 = l4;

    private const ulong MASK51 = (1UL << 51) - 1;

    // Константы
    public static readonly FieldElement Zero = new FieldElement(0, 0, 0, 0, 0);
    public static readonly FieldElement One = new FieldElement(1, 0, 0, 0, 0);
    public static readonly FieldElement A24 = new FieldElement(121665, 0, 0, 0, 0);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldElement Add(in FieldElement a, in FieldElement b)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            ref ulong pA = ref Unsafe.As<FieldElement, ulong>(ref Unsafe.AsRef(in a));
            ref ulong pB = ref Unsafe.As<FieldElement, ulong>(ref Unsafe.AsRef(in b));
            
            // Загружаем первые 4 лимба (256 бит = 32 байта) в векторы
            Vector256<ulong> va = Vector256.LoadUnsafe(ref pA);
            Vector256<ulong> vb = Vector256.LoadUnsafe(ref pB);
            
            // Сложение 4 лимбов за 1 такт CPU
            Vector256<ulong> vr = va + vb;
            
            // 5-й лимб складываем скалярно
            ulong l4 = a.L4 + b.L4;
            
            FieldElement res = default;
            ref ulong pRes = ref Unsafe.As<FieldElement, ulong>(ref res);
            vr.StoreUnsafe(ref pRes);
            Unsafe.Add(ref pRes, 4) = l4;
            
            return res;
        }
        
        // Fallback для систем без AVX2
        return new FieldElement(
            a.L0 + b.L0,
            a.L1 + b.L1,
            a.L2 + b.L2,
            a.L3 + b.L3,
            a.L4 + b.L4
        );
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldElement Sub(in FieldElement a, in FieldElement b)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            ref ulong pA = ref Unsafe.As<FieldElement, ulong>(ref Unsafe.AsRef(in a));
            ref ulong pB = ref Unsafe.As<FieldElement, ulong>(ref Unsafe.AsRef(in b));
            
            // Константа 2p разбитая на 4 лимба для вектора
            Vector256<ulong> v2p = Vector256.Create(
                0xFFFFFFFFFFFDAUL,
                0xFFFFFFFFFFFFEUL,
                0xFFFFFFFFFFFFEUL,
                0xFFFFFFFFFFFFEUL
            );
            
            Vector256<ulong> va = Vector256.LoadUnsafe(ref pA);
            Vector256<ulong> vb = Vector256.LoadUnsafe(ref pB);
            
            // SIMD-вычитание: vr = (va + 2p) - vb
            Vector256<ulong> vr = (va + v2p) - vb;
            
            // 5-й лимб обрабатываем скалярно
            ulong l4 = (a.L4 + 0xFFFFFFFFFFFFE) - b.L4;
            
            FieldElement res = default;
            ref ulong pRes = ref Unsafe.As<FieldElement, ulong>(ref res);
            Vector256.StoreUnsafe(vr, ref pRes);
            Unsafe.Add(ref pRes, 4) = l4;
            
            return res;
        }
        
        unchecked
        {
            return new FieldElement(
                (a.L0 + 0xFFFFFFFFFFFDA) - b.L0,
                (a.L1 + 0xFFFFFFFFFFFFE) - b.L1,
                (a.L2 + 0xFFFFFFFFFFFFE) - b.L2,
                (a.L3 + 0xFFFFFFFFFFFFE) - b.L3,
                (a.L4 + 0xFFFFFFFFFFFFE) - b.L4
            );
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldElement Multiply(in FieldElement f, in FieldElement g)
    {
        ulong f0 = f.L0, f1 = f.L1, f2 = f.L2, f3 = f.L3, f4 = f.L4;
        ulong g0 = g.L0, g1 = g.L1, g2 = g.L2, g3 = g.L3, g4 = g.L4;

        ulong g1_19 = g1 * 19; ulong g2_19 = g2 * 19;
        ulong g3_19 = g3 * 19; ulong g4_19 = g4 * 19;

        UInt128 r0 = (UInt128)f0 * g0 + (UInt128)f1 * g4_19 + (UInt128)f2 * g3_19 + (UInt128)f3 * g2_19 + (UInt128)f4 * g1_19;
        UInt128 r1 = (UInt128)f0 * g1 + (UInt128)f1 * g0 + (UInt128)f2 * g4_19 + (UInt128)f3 * g3_19 + (UInt128)f4 * g2_19;
        UInt128 r2 = (UInt128)f0 * g2 + (UInt128)f1 * g1 + (UInt128)f2 * g0 + (UInt128)f3 * g4_19 + (UInt128)f4 * g3_19;
        UInt128 r3 = (UInt128)f0 * g3 + (UInt128)f1 * g2 + (UInt128)f2 * g1 + (UInt128)f3 * g0 + (UInt128)f4 * g4_19;
        UInt128 r4 = (UInt128)f0 * g4 + (UInt128)f1 * g3 + (UInt128)f2 * g2 + (UInt128)f3 * g1 + (UInt128)f4 * g0;

        ulong c;
        ulong h0 = (ulong)(r0 & MASK51); c = (ulong)(r0 >> 51); r1 += c;
        ulong h1 = (ulong)(r1 & MASK51); c = (ulong)(r1 >> 51); r2 += c;
        ulong h2 = (ulong)(r2 & MASK51); c = (ulong)(r2 >> 51); r3 += c;
        ulong h3 = (ulong)(r3 & MASK51); c = (ulong)(r3 >> 51); r4 += c;
        ulong h4 = (ulong)(r4 & MASK51); c = (ulong)(r4 >> 51);

        h0 += c * 19;
        c = h0 >> 51; h0 &= MASK51; h1 += c;

        return new FieldElement(h0, h1, h2, h3, h4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldElement Square(in FieldElement f)
    {
        ulong f0 = f.L0, f1 = f.L1, f2 = f.L2, f3 = f.L3, f4 = f.L4;
        ulong f0_2 = f0 * 2, f1_2 = f1 * 2;
        ulong f3_19 = f3 * 19, f4_19 = f4 * 19;
        ulong f4_38 = f4 * 38, f3_38 = f3 * 38;

        UInt128 r0 = (UInt128)f0 * f0 + (UInt128)f1 * f4_38 + (UInt128)f2 * f3_38;
        UInt128 r1 = (UInt128)f0_2 * f1 + (UInt128)f2 * f4_38 + (UInt128)f3 * f3_19;
        UInt128 r2 = (UInt128)f0_2 * f2 + (UInt128)f1 * f1 + (UInt128)f3 * f4_38;
        UInt128 r3 = (UInt128)f0_2 * f3 + (UInt128)f1_2 * f2 + (UInt128)f4 * f4_19;
        UInt128 r4 = (UInt128)f0_2 * f4 + (UInt128)f1_2 * f3 + (UInt128)f2 * f2;

        ulong h0 = (ulong)(r0 & MASK51); ulong c0 = (ulong)(r0 >> 51); r1 += c0;
        ulong h1 = (ulong)(r1 & MASK51); ulong c1 = (ulong)(r1 >> 51); r2 += c1;
        ulong h2 = (ulong)(r2 & MASK51); ulong c2 = (ulong)(r2 >> 51); r3 += c2;
        ulong h3 = (ulong)(r3 & MASK51); ulong c3 = (ulong)(r3 >> 51); r4 += c3;
        ulong h4 = (ulong)(r4 & MASK51); ulong c4 = (ulong)(r4 >> 51);

        h0 += c4 * 19;
        ulong cFinal = h0 >> 51; h0 &= MASK51; h1 += cFinal;

        return new FieldElement(h0, h1, h2, h3, h4);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CSwap(ref FieldElement a, ref FieldElement b, int swap)
    {
        // Если swap = 1, mask = 0xFFFFFFFFFFFFFFFF. Если 0, mask = 0
        ulong mask = (ulong)-swap;

        if (Vector256.IsHardwareAccelerated)
        {
            ref ulong pA = ref Unsafe.As<FieldElement, ulong>(ref a);
            ref ulong pB = ref Unsafe.As<FieldElement, ulong>(ref b);
            
            Vector256<ulong> vMask = Vector256.Create(mask);
            Vector256<ulong> va = Vector256.LoadUnsafe(ref pA);
            Vector256<ulong> vb = Vector256.LoadUnsafe(ref pB);
            
            // dummy = mask & (a ^ b)
            Vector256<ulong> dummy = vMask & (va ^ vb);
            
            // a = a ^ dummy, b = b ^ dummy
            (va ^ dummy).StoreUnsafe(ref pA);
            (vb ^ dummy).StoreUnsafe(ref pB);
            
            // 5-й лимб скалярно
            ulong dummy4 = mask & (a.L4 ^ b.L4);
            Unsafe.Add(ref pA, 4) ^= dummy4;
            Unsafe.Add(ref pB, 4) ^= dummy4;
        }
        else
        {
            ulong t0 = mask & (a.L0 ^ b.L0);
            ulong t1 = mask & (a.L1 ^ b.L1);
            ulong t2 = mask & (a.L2 ^ b.L2);
            ulong t3 = mask & (a.L3 ^ b.L3);
            ulong t4 = mask & (a.L4 ^ b.L4);
            
            a = new FieldElement(a.L0 ^ t0, a.L1 ^ t1, a.L2 ^ t2, a.L3 ^ t3, a.L4 ^ t4);
            b = new FieldElement(b.L0 ^ t0, b.L1 ^ t1, b.L2 ^ t2, b.L3 ^ t3, b.L4 ^ t4);
        }
    }

    // <summary>
    /// Вычисляет обратный элемент (z^(-1)) по модулю 2^255-19.
    /// Использует Малую теорему Ферма: z^(p-2).
    /// </summary>
    public static FieldElement Invert(in FieldElement z)
    {

        FieldElement _2 = Square(z); // 2
        FieldElement _4 = Square(_2); // 4
        FieldElement _8 = Square(_4); // 8
        FieldElement _9 = Multiply(_8, z); // 9
        FieldElement _11 = Multiply(_9, _2); // 11
        FieldElement _22 = Square(_11); // 22
        FieldElement _2_5_0 = Multiply(_22, _9); // 2^5 - 2^0 = 31
        
        FieldElement _2_10_0 = Square(_2_5_0); // 2^6
        for (int i = 0; i < 4; i++) _2_10_0 = Square(_2_10_0); // 2^10
        _2_10_0 = Multiply(_2_10_0, _2_5_0); // 2^10 - 2^0
        
        FieldElement _2_20_0 = Square(_2_10_0);
        for (int i = 0; i < 9; i++) _2_20_0 = Square(_2_20_0);
        _2_20_0 = Multiply(_2_20_0, _2_10_0); // 2^20 - 2^0
        
        FieldElement _2_40_0 = Square(_2_20_0);
        for (int i = 0; i < 19; i++) _2_40_0 = Square(_2_40_0);
        _2_40_0 = Multiply(_2_40_0, _2_20_0); // 2^40 - 2^0
        
        FieldElement _2_50_0 = Square(_2_40_0);
        for (int i = 0; i < 9; i++) _2_50_0 = Square(_2_50_0);
        _2_50_0 = Multiply(_2_50_0, _2_10_0); // 2^50 - 2^0
        
        FieldElement _2_100_0 = Square(_2_50_0);
        for (int i = 0; i < 49; i++) _2_100_0 = Square(_2_100_0);
        _2_100_0 = Multiply(_2_100_0, _2_50_0); // 2^100 - 2^0
        
        FieldElement _2_200_0 = Square(_2_100_0);
        for (int i = 0; i < 99; i++) _2_200_0 = Square(_2_200_0);
        _2_200_0 = Multiply(_2_200_0, _2_100_0); // 2^200 - 2^0
        
        FieldElement _2_250_0 = Square(_2_200_0);
        for (int i = 0; i < 49; i++) _2_250_0 = Square(_2_250_0);
        _2_250_0 = Multiply(_2_250_0, _2_50_0); // 2^250 - 2^0
        
        FieldElement _2_255_21 = Square(_2_250_0);
        for (int i = 0; i < 4; i++) _2_255_21 = Square(_2_255_21); // 2^255
        _2_255_21 = Multiply(_2_255_21, _11); // умножаем на z^11

        return _2_255_21;
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
        c = h4 >> 51; h4 &= MASK51;
        h0 += c * 19;

        c = h0 >> 51; h0 &= MASK51; h1 += c;
        c = h1 >> 51; h1 &= MASK51; h2 += c;
        c = h2 >> 51; h2 &= MASK51; h3 += c;
        c = h3 >> 51; h3 &= MASK51; h4 += c;

        // Теперь значение h гарантированно меньше 2p, но может быть >= p.
        // Вычислим h_minus_p = h - p = h - (2^255 - 19) = h - 2^255 + 19.
        ulong s0 = h0 + 19;
        c = s0 >> 51; s0 &= MASK51;
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

