using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Core.Math;

namespace Core;

public static class BatchedX25519
{
    public static void Multiply4(
        ReadOnlySpan<byte> k0, ReadOnlySpan<byte> u0, Span<byte> out0,
        ReadOnlySpan<byte> k1, ReadOnlySpan<byte> u1, Span<byte> out1,
        ReadOnlySpan<byte> k2, ReadOnlySpan<byte> u2, Span<byte> out2,
        ReadOnlySpan<byte> k3, ReadOnlySpan<byte> u3, Span<byte> out3)
    {
        // 1. Clamping с помощью 64-битных регистров, избегая деления и byte-массивов внутри цикла
        Span<ulong> s0 = stackalloc ulong[4];
        Span<ulong> s1 = stackalloc ulong[4];
        Span<ulong> s2 = stackalloc ulong[4];
        Span<ulong> s3 = stackalloc ulong[4];

        k0.CopyTo(MemoryMarshal.Cast<ulong, byte>(s0));
        k1.CopyTo(MemoryMarshal.Cast<ulong, byte>(s1));
        k2.CopyTo(MemoryMarshal.Cast<ulong, byte>(s2));
        k3.CopyTo(MemoryMarshal.Cast<ulong, byte>(s3));

        Clamp(s0); Clamp(s1); Clamp(s2); Clamp(s3);

        // 2. Инициализация переменных In-Place (никаких аллокаций new внутри)
        BatchedFieldElement x_1 = BatchedFieldElement.FromBytes4(u0, u1, u2, u3);
        BatchedFieldElement x_2 = BatchedFieldElement.One;
        BatchedFieldElement z_2 = BatchedFieldElement.Zero;
        BatchedFieldElement x_3 = x_1;
        BatchedFieldElement z_3 = BatchedFieldElement.One;

        Vector256<ulong> swap = Vector256<ulong>.Zero;

        // Предварительно выделенные переменные для переиспользования в лестнице (In-Place)
        BatchedFieldElement A = default;
        BatchedFieldElement AA = default;
        BatchedFieldElement B = default;
        BatchedFieldElement BB = default;
        BatchedFieldElement E = default;
        BatchedFieldElement C = default;
        BatchedFieldElement D = default;
        BatchedFieldElement DA = default;
        BatchedFieldElement CB = default;

        BatchedFieldElement localA24 = BatchedFieldElement.A24; // Избегаем defensive copy для read-only константы

        // 3. Главный цикл лестницы
        for (int t = 254; t >= 0; t--)
        {
            // Ускоренное извлечение бита через битовые сдвиги над массивом ulong
            ulong bit0 = (s0[t >> 6] >> (t & 63)) & 1;
            ulong bit1 = (s1[t >> 6] >> (t & 63)) & 1;
            ulong bit2 = (s2[t >> 6] >> (t & 63)) & 1;
            ulong bit3 = (s3[t >> 6] >> (t & 63)) & 1;

            Vector256<ulong> k_t = Vector256.Create(bit0, bit1, bit2, bit3);

            swap ^= k_t;

            // Избегаем медленных вызовов GetElement. За счет того, что вектор состоит из 0 и 1,
            // (0 - 1) дает 0xFFFFFFFFFFFFFFFF, а (0 - 0) дает 0, что идеально для битовой маски.
            Vector256<ulong> swapMask = Vector256<ulong>.Zero - swap;

            BatchedFieldElement.CSwap(ref x_2, ref x_3, swapMask);
            BatchedFieldElement.CSwap(ref z_2, ref z_3, swapMask);
            
            swap = k_t;

            // --- Полная In-Place Арифметика Монтгомери ---
            BatchedFieldElement.Add(ref A, in x_2, in z_2);
            BatchedFieldElement.Square(ref AA, in A);
            BatchedFieldElement.Sub(ref B, in x_2, in z_2);
            BatchedFieldElement.Square(ref BB, in B);
            
            BatchedFieldElement.Sub(ref E, in AA, in BB);
            
            BatchedFieldElement.Add(ref C, in x_3, in z_3);
            BatchedFieldElement.Sub(ref D, in x_3, in z_3);
            
            BatchedFieldElement.Multiply(ref DA, in D, in A);
            BatchedFieldElement.Multiply(ref CB, in C, in B);
            
            // x_3 = (DA + CB)^2
            BatchedFieldElement.Add(ref x_3, in DA, in CB);
            BatchedFieldElement.Square(ref x_3, in x_3);
            
            // z_3 = x_1 * (DA - CB)^2
            BatchedFieldElement.Sub(ref z_3, in DA, in CB);
            BatchedFieldElement.Square(ref z_3, in z_3);
            BatchedFieldElement.Multiply(ref z_3, in x_1, in z_3);
            
            // x_2 = AA * BB
            BatchedFieldElement.Multiply(ref x_2, in AA, in BB);
            
            // z_2 = E * (AA + a24 * E)
            BatchedFieldElement.Multiply(ref z_2, in localA24, in E);
            BatchedFieldElement.Add(ref z_2, in AA, in z_2);
            BatchedFieldElement.Multiply(ref z_2, in E, in z_2);
        }

        Vector256<ulong> finalSwapMask = Vector256<ulong>.Zero - swap;
        BatchedFieldElement.CSwap(ref x_2, ref x_3, finalSwapMask);
        BatchedFieldElement.CSwap(ref z_2, ref z_3, finalSwapMask);

        // Расчет итогов
        BatchedFieldElement z_2_inv = default;
        BatchedFieldElement.Invert(ref z_2_inv, in z_2);
        
        BatchedFieldElement result = default;
        BatchedFieldElement.Multiply(ref result, in x_2, in z_2_inv);

        result.ToBytes4(out0, out1, out2, out3);
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Clamp(Span<ulong> k)
    {
        // Эквивалентно побайтовому клампингу благодаря little-endian: 
        // k[0] &= 248 -> сброс 3х бит в младшем байте 1-го элемента 
        // k[31] &= 127; k[31] |= 64 -> операции над старшим байтом 4-го элемента
        k[0] &= 0xFFFFFFFFFFFFFFF8UL;
        k[3] &= 0x7FFFFFFFFFFFFFFFUL;
        k[3] |= 0x4000000000000000UL;
    }
}