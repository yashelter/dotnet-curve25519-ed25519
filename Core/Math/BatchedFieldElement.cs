namespace Core.Math;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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
    
    public static readonly BatchedFieldElement Zero = new (
        Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly BatchedFieldElement One = new (
        Vector256.Create(1UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);
    
    public static readonly BatchedFieldElement A24 = new (
        Vector256.Create(121665UL), Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero, Vector256<ulong>.Zero);

    // Полностью векторное сложение (4 точки за 1 шаг)[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Add(in BatchedFieldElement a, in BatchedFieldElement b)
    {
        return new BatchedFieldElement(
            a.L0 + b.L0,
            a.L1 + b.L1,
            a.L2 + b.L2,
            a.L3 + b.L3,
            a.L4 + b.L4
        );
    }

    // Полностью векторное вычитание (4 точки за 1 шаг)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Sub(in BatchedFieldElement a, in BatchedFieldElement b)
    {
        Vector256<ulong> v2p0 = Vector256.Create(0xFFFFFFFFFFFDAUL);
        Vector256<ulong> v2p14 = Vector256.Create(0xFFFFFFFFFFFFEUL);

        return new BatchedFieldElement(
            (a.L0 + v2p0) - b.L0,
            (a.L1 + v2p14) - b.L1,
            (a.L2 + v2p14) - b.L2,
            (a.L3 + v2p14) - b.L3,
            (a.L4 + v2p14) - b.L4
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CSwap(ref BatchedFieldElement a, ref BatchedFieldElement b, Vector256<ulong> mask)
    {
        Vector256<ulong> dummy0 = mask & (a.L0 ^ b.L0);
        Vector256<ulong> dummy1 = mask & (a.L1 ^ b.L1);
        Vector256<ulong> dummy2 = mask & (a.L2 ^ b.L2);
        Vector256<ulong> dummy3 = mask & (a.L3 ^ b.L3);
        Vector256<ulong> dummy4 = mask & (a.L4 ^ b.L4);

        a.L0 ^= dummy0;
        a.L1 ^= dummy1;
        a.L2 ^= dummy2;
        a.L3 ^= dummy3; 
        a.L4 ^= dummy4;
        
        b.L0 ^= dummy0;
        b.L1 ^= dummy1;
        b.L2 ^= dummy2; 
        b.L3 ^= dummy3;
        b.L4 ^= dummy4;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Multiply(in BatchedFieldElement a, in BatchedFieldElement b)
    {
        Vector256<ulong> b1_19 = b.L1 * 19;
        Vector256<ulong> b2_19 = b.L2 * 19;
        Vector256<ulong> b3_19 = b.L3 * 19;
        Vector256<ulong> b4_19 = b.L4 * 19;
        
        Vector256<ulong> res0 = default, res1 = default, res2 = default, res3 = default, res4 = default;
        
        ref ulong out0 = ref Unsafe.As<Vector256<ulong>, ulong>(ref res0);
        ref ulong out1 = ref Unsafe.As<Vector256<ulong>, ulong>(ref res1);
        ref ulong out2 = ref Unsafe.As<Vector256<ulong>, ulong>(ref res2);
        ref ulong out3 = ref Unsafe.As<Vector256<ulong>, ulong>(ref res3);
        ref ulong out4 = ref Unsafe.As<Vector256<ulong>, ulong>(ref res4);
        
        const ulong M51 = (1UL << 51) - 1;
        
        for (int i = 0; i < 4; i++)
        {
            // Читаем значения лимб текущего элемента пачки в регистры
            ulong f0 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L0)), i);
            ulong f1 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L1)), i);
            ulong f2 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L2)), i);
            ulong f3 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L3)), i);
            ulong f4 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in a.L4)), i);
            
            ulong g0 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L0)), i);
            ulong g1 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L1)), i);
            ulong g2 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L2)), i);
            ulong g3 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L3)), i);
            ulong g4 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in b.L4)), i);
            
            // Читаем предвычисленные значения * 19
            ulong g1_19 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref b1_19), i);
            ulong g2_19 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref b2_19), i);
            ulong g3_19 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref b3_19), i);
            ulong g4_19 = Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref b4_19), i);
            
            // Используем ОДНУ переменную UInt128 как аккумулятор.
            // Это критически важно: JIT выделит под неё RAX:RDX.
            
            // r0
            UInt128 acc = (UInt128)f0 * g0;
            acc += (UInt128)f1 * g4_19;
            acc += (UInt128)f2 * g3_19;
            acc += (UInt128)f3 * g2_19;
            acc += (UInt128)f4 * g1_19;
            
            ulong h0 = (ulong)acc & M51;
            ulong c = (ulong)(acc >> 51);
            
            // r1
            acc = (UInt128)f0 * g1;
            acc += (UInt128)f1 * g0;
            acc += (UInt128)f2 * g4_19;
            acc += (UInt128)f3 * g3_19;
            acc += (UInt128)f4 * g2_19;
            acc += c;
            
            ulong h1 = (ulong)acc & M51;
            c = (ulong)(acc >> 51);
            
            // r2
            acc = (UInt128)f0 * g2;
            acc += (UInt128)f1 * g1;
            acc += (UInt128)f2 * g0;
            acc += (UInt128)f3 * g4_19;
            acc += (UInt128)f4 * g3_19;
            acc += c;
            
            ulong h2 = (ulong)acc & M51;
            c = (ulong)(acc >> 51);
            
            // r3
            acc = (UInt128)f0 * g3;
            acc += (UInt128)f1 * g2;
            acc += (UInt128)f2 * g1;
            acc += (UInt128)f3 * g0;
            acc += (UInt128)f4 * g4_19;
            acc += c;
            
            ulong h3 = (ulong)acc & M51;
            c = (ulong)(acc >> 51);
            
            // r4
            acc = (UInt128)f0 * g4;
            acc += (UInt128)f1 * g3;
            acc += (UInt128)f2 * g2;
            acc += (UInt128)f3 * g1;
            acc += (UInt128)f4 * g0;
            acc += c;
            
            ulong h4 = (ulong)acc & M51;
            c = (ulong)(acc >> 51);
            
            // Финальная редукция (carry back)
            h0 += c * 19;
            c = h0 >> 51;
            h0 &= M51;
            h1 += c;
            
            // Записываем напрямую в память результата
            Unsafe.Add(ref out0, i) = h0;
            Unsafe.Add(ref out1, i) = h1;
            Unsafe.Add(ref out2, i) = h2;
            Unsafe.Add(ref out3, i) = h3;
            Unsafe.Add(ref out4, i) = h4;
        }
        
        return new BatchedFieldElement(res0, res1, res2, res3, res4);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Square(in BatchedFieldElement f)
    {
        Span<ulong> outL0 = stackalloc ulong[4];
        Span<ulong> outL1 = stackalloc ulong[4];
        Span<ulong> outL2 = stackalloc ulong[4];
        Span<ulong> outL3 = stackalloc ulong[4];
        Span<ulong> outL4 = stackalloc ulong[4];

        ref ulong f0_ref = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L0));
        ref ulong f1_ref = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L1));
        ref ulong f2_ref = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L2));
        ref ulong f3_ref = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L3));
        ref ulong f4_ref = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in f.L4));

        for (int i = 0; i < 4; i++)
        {
            ulong f0 = Unsafe.Add(ref f0_ref, i), f1 = Unsafe.Add(ref f1_ref, i), f2 = Unsafe.Add(ref f2_ref, i), f3 = Unsafe.Add(ref f3_ref, i), f4 = Unsafe.Add(ref f4_ref, i);
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

            outL0[i] = h0; outL1[i] = h1; outL2[i] = h2; outL3[i] = h3; outL4[i] = h4;
        }

        return new BatchedFieldElement(
            Vector256.Create(outL0[0], outL0[1], outL0[2], outL0[3]),
            Vector256.Create(outL1[0], outL1[1], outL1[2], outL1[3]),
            Vector256.Create(outL2[0], outL2[1], outL2[2], outL2[3]),
            Vector256.Create(outL3[0], outL3[1], outL3[2], outL3[3]),
            Vector256.Create(outL4[0], outL4[1], outL4[2], outL4[3])
        );
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SquareInline(ref BatchedFieldElement f)
    {
        Span<ulong> r0 = stackalloc ulong[4];
        Span<ulong> r1 = stackalloc ulong[4];
        Span<ulong> r2 = stackalloc ulong[4];
        Span<ulong> r3 = stackalloc ulong[4];
        Span<ulong> r4 = stackalloc ulong[4];
        
        // Получаем ссылки на начало каждого вектора (L0...L4)
        ref ulong f0_ptr = ref Unsafe.As<Vector256<ulong>, ulong>(ref f.L0);
        ref ulong f1_ptr = ref Unsafe.As<Vector256<ulong>, ulong>(ref f.L1);
        ref ulong f2_ptr = ref Unsafe.As<Vector256<ulong>, ulong>(ref f.L2);
        ref ulong f3_ptr = ref Unsafe.As<Vector256<ulong>, ulong>(ref f.L3);
        ref ulong f4_ptr = ref Unsafe.As<Vector256<ulong>, ulong>(ref f.L4);
        
        for (int i = 0; i < 4; i++)
        {
            // Читаем значения i-й линии
            ulong f0 = Unsafe.Add(ref f0_ptr, i);
            ulong f1 = Unsafe.Add(ref f1_ptr, i);
            ulong f2 = Unsafe.Add(ref f2_ptr, i);
            ulong f3 = Unsafe.Add(ref f3_ptr, i);
            ulong f4 = Unsafe.Add(ref f4_ptr, i);
            
            // Арифметика (без изменений, она оптимальна через UInt128)
            ulong f0_2 = f0 * 2, f1_2 = f1 * 2;
            ulong f3_19 = f3 * 19, f4_19 = f4 * 19;
            ulong f4_38 = f4 * 38, f3_38 = f3 * 38;
            
            UInt128 res0 = (UInt128)f0 * f0 + (UInt128)f1 * f4_38 + (UInt128)f2 * f3_38;
            UInt128 res1 = (UInt128)f0_2 * f1 + (UInt128)f2 * f4_38 + (UInt128)f3 * f3_19;
            UInt128 res2 = (UInt128)f0_2 * f2 + (UInt128)f1 * f1 + (UInt128)f3 * f4_38;
            UInt128 res3 = (UInt128)f0_2 * f3 + (UInt128)f1_2 * f2 + (UInt128)f4 * f4_19;
            UInt128 res4 = (UInt128)f0_2 * f4 + (UInt128)f1_2 * f3 + (UInt128)f2 * f2;
            
            // Carry propagation
            ulong c;
            ulong h0 = (ulong)(res0 & MASK51);
            c = (ulong)(res0 >> 51);
            res1 += c;
            ulong h1 = (ulong)(res1 & MASK51);
            c = (ulong)(res1 >> 51);
            res2 += c;
            ulong h2 = (ulong)(res2 & MASK51);
            c = (ulong)(res2 >> 51);
            res3 += c;
            ulong h3 = (ulong)(res3 & MASK51);
            c = (ulong)(res3 >> 51);
            res4 += c;
            ulong h4 = (ulong)(res4 & MASK51);
            c = (ulong)(res4 >> 51);
            
            h0 += c * 19;
            c = h0 >> 51;
            h0 &= MASK51;
            h1 += c;
            
            // Сохраняем результат во временный буфер
            r0[i] = h0;
            r1[i] = h1;
            r2[i] = h2;
            r3[i] = h3;
            r4[i] = h4;
        }
        
        f.L0 = Vector256.Create(r0[0], r0[1], r0[2], r0[3]);
        f.L1 = Vector256.Create(r1[0], r1[1], r1[2], r1[3]);
        f.L2 = Vector256.Create(r2[0], r2[1], r2[2], r2[3]);
        f.L3 = Vector256.Create(r3[0], r3[1], r3[2], r3[3]);
        f.L4 = Vector256.Create(r4[0], r4[1], r4[2], r4[3]);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SquareMany(ref BatchedFieldElement x, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SquareInline(ref x);
        }
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Invert(in BatchedFieldElement z)
    {
        BatchedFieldElement _2 = Square(z);
        BatchedFieldElement _4 = Square(_2);
        BatchedFieldElement _8 = Square(_4);
        BatchedFieldElement _9 = Multiply(_8, z);
        BatchedFieldElement _11 = Multiply(_9, _2);
        BatchedFieldElement _22 = Square(_11);
        BatchedFieldElement _2_5_0 = Multiply(_22, _9);
        
        BatchedFieldElement _2_10_0 = Square(_2_5_0);
        SquareMany(ref _2_10_0, 4);
        _2_10_0 = Multiply(_2_10_0, _2_5_0);
        
        BatchedFieldElement _2_20_0 = Square(_2_10_0);
        SquareMany(ref _2_20_0, 9);
        _2_20_0 = Multiply(_2_20_0, _2_10_0);
        
        BatchedFieldElement _2_40_0 = Square(_2_20_0);
        SquareMany(ref _2_40_0, 19);
        _2_40_0 = Multiply(_2_40_0, _2_20_0);
        
        BatchedFieldElement _2_50_0 = Square(_2_40_0);
        SquareMany(ref _2_50_0, 9);
        _2_50_0 = Multiply(_2_50_0, _2_10_0);
        
        BatchedFieldElement _2_100_0 = Square(_2_50_0);
        SquareMany(ref _2_100_0, 49);
        _2_100_0 = Multiply(_2_100_0, _2_50_0);
        
        BatchedFieldElement _2_200_0 = Square(_2_100_0);
        SquareMany(ref _2_200_0, 99);
        _2_200_0 = Multiply(_2_200_0, _2_100_0);
        
        BatchedFieldElement _2_250_0 = Square(_2_200_0);
        SquareMany(ref _2_250_0, 49);
        _2_250_0 = Multiply(_2_250_0, _2_50_0);
        
        BatchedFieldElement _2_255_21 = Square(_2_250_0);
        SquareMany(ref _2_255_21, 4);
        _2_255_21 = Multiply(_2_255_21, _11);

        return _2_255_21;
    }
    /// <summary>
    /// Загружает 4 публичных ключа в один батч
    /// </summary>
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
    
    /// <summary>
    /// Выгружает 4 результата в соответствующие массивы байт
    /// </summary>
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
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchedFieldElement Multiply0(in BatchedFieldElement a, in BatchedFieldElement b)
    {
        // Выделяем память на стеке для результата (4 слота по 5 лимбов = 20 ulong)
        Span<ulong> outL0 = stackalloc ulong[4];
        Span<ulong> outL1 = stackalloc ulong[4];
        Span<ulong> outL2 = stackalloc ulong[4];
        Span<ulong> outL3 = stackalloc ulong[4];
        Span<ulong> outL4 = stackalloc ulong[4];

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
        
        for (int i = 0; i < 4; i++)
        {
            ulong f0 = Unsafe.Add(ref a0, i), f1 = Unsafe.Add(ref a1, i), f2 = Unsafe.Add(ref a2, i), f3 = Unsafe.Add(ref a3, i), f4 = Unsafe.Add(ref a4, i);
            ulong g0 = Unsafe.Add(ref b0, i), g1 = Unsafe.Add(ref b1, i), g2 = Unsafe.Add(ref b2, i), g3 = Unsafe.Add(ref b3, i), g4 = Unsafe.Add(ref b4, i);

            ulong g1_19 = g1 * 19, g2_19 = g2 * 19, g3_19 = g3 * 19, g4_19 = g4 * 19;

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

            outL0[i] = h0; outL1[i] = h1; outL2[i] = h2; outL3[i] = h3; outL4[i] = h4;
        }

        return new BatchedFieldElement(
            Vector256.Create(outL0[0], outL0[1], outL0[2], outL0[3]),
            Vector256.Create(outL1[0], outL1[1], outL1[2], outL1[3]),
            Vector256.Create(outL2[0], outL2[1], outL2[2], outL2[3]),
            Vector256.Create(outL3[0], outL3[1], outL3[2], outL3[3]),
            Vector256.Create(outL4[0], outL4[1], outL4[2], outL4[3])
        );
    }
    
}