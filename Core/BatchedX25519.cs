namespace Core.Math;

using System;
using System.Runtime.Intrinsics;

public static class BatchedX25519
{
    /// <summary>
    /// Вычисляет X25519 параллельно для 4 пар (скаляр, u-координата).
    /// </summary>
    public static void Multiply4(
        ReadOnlySpan<byte> k0, ReadOnlySpan<byte> u0, Span<byte> out0,
        ReadOnlySpan<byte> k1, ReadOnlySpan<byte> u1, Span<byte> out1,
        ReadOnlySpan<byte> k2, ReadOnlySpan<byte> u2, Span<byte> out2,
        ReadOnlySpan<byte> k3, ReadOnlySpan<byte> u3, Span<byte> out3)
    {
        // 1. Clamping для всех 4 секретных ключей
        Span<byte> scalar0 = stackalloc byte[32]; k0.CopyTo(scalar0); Clamp(scalar0);
        Span<byte> scalar1 = stackalloc byte[32]; k1.CopyTo(scalar1); Clamp(scalar1);
        Span<byte> scalar2 = stackalloc byte[32]; k2.CopyTo(scalar2); Clamp(scalar2);
        Span<byte> scalar3 = stackalloc byte[32]; k3.CopyTo(scalar3); Clamp(scalar3);

        // 2. Инициализация (Batched)
        BatchedFieldElement x_1 = BatchedFieldElement.FromBytes4(u0, u1, u2, u3);
        BatchedFieldElement x_2 = BatchedFieldElement.One;
        BatchedFieldElement z_2 = BatchedFieldElement.Zero;
        BatchedFieldElement x_3 = x_1;
        BatchedFieldElement z_3 = BatchedFieldElement.One;

        Vector256<ulong> swap = Vector256<ulong>.Zero;

        // 3. Главный цикл лестницы
        for (int t = 254; t >= 0; t--)
        {
            // Извлекаем бит номер 't' для каждого из 4 ключей
            ulong bit0 = (ulong)((scalar0[t / 8] >> (t % 8)) & 1);
            ulong bit1 = (ulong)((scalar1[t / 8] >> (t % 8)) & 1);
            ulong bit2 = (ulong)((scalar2[t / 8] >> (t % 8)) & 1);
            ulong bit3 = (ulong)((scalar3[t / 8] >> (t % 8)) & 1);

            // Создаем вектор текущих битов (1 или 0)
            Vector256<ulong> k_t = Vector256.Create(bit0, bit1, bit2, bit3);

            // swap ^= k_t
            swap ^= k_t;

            // Формируем векторную маску: если бит = 1, то ulong = 0xFF...FF, иначе 0
            Vector256<ulong> swapMask = Vector256.Create(
                (ulong)-(long)swap.GetElement(0),
                (ulong)-(long)swap.GetElement(1),
                (ulong)-(long)swap.GetElement(2),
                (ulong)-(long)swap.GetElement(3)
            );

            // Константный обмен для всех 4 точек одновременно!
            BatchedFieldElement.CSwap(ref x_2, ref x_3, swapMask);
            BatchedFieldElement.CSwap(ref z_2, ref z_3, swapMask);
            
            swap = k_t;

            // --- Арифметика Монтгомери (работает сразу для 4 точек) ---
            BatchedFieldElement A = BatchedFieldElement.Add(x_2, z_2);
            BatchedFieldElement AA = BatchedFieldElement.Square(A);
            BatchedFieldElement B = BatchedFieldElement.Sub(x_2, z_2);
            BatchedFieldElement BB = BatchedFieldElement.Square(B);
            
            BatchedFieldElement E = BatchedFieldElement.Sub(AA, BB);
            
            BatchedFieldElement C = BatchedFieldElement.Add(x_3, z_3);
            BatchedFieldElement D = BatchedFieldElement.Sub(x_3, z_3);
            
            BatchedFieldElement DA = BatchedFieldElement.Multiply(D, A);
            BatchedFieldElement CB = BatchedFieldElement.Multiply(C, B);
            
            BatchedFieldElement DA_plus_CB = BatchedFieldElement.Add(DA, CB);
            x_3 = BatchedFieldElement.Square(DA_plus_CB);
            
            BatchedFieldElement DA_minus_CB = BatchedFieldElement.Sub(DA, CB);
            BatchedFieldElement DA_minus_CB_sq = BatchedFieldElement.Square(DA_minus_CB);
            z_3 = BatchedFieldElement.Multiply(x_1, DA_minus_CB_sq);
            
            x_2 = BatchedFieldElement.Multiply(AA, BB);
            
            BatchedFieldElement a24_E = BatchedFieldElement.Multiply(BatchedFieldElement.A24, E);
            BatchedFieldElement AA_plus_a24_E = BatchedFieldElement.Add(AA, a24_E);
            z_2 = BatchedFieldElement.Multiply(E, AA_plus_a24_E);
        }

        // Финальный swap mask
        Vector256<ulong> finalSwapMask = Vector256.Create(
            (ulong)-(long)swap.GetElement(0),
            (ulong)-(long)swap.GetElement(1),
            (ulong)-(long)swap.GetElement(2),
            (ulong)-(long)swap.GetElement(3)
        );

        BatchedFieldElement.CSwap(ref x_2, ref x_3, finalSwapMask);
        BatchedFieldElement.CSwap(ref z_2, ref z_3, finalSwapMask);

        // Инверсия z_2 для 4 точек одновременно
        BatchedFieldElement z_2_inv = BatchedFieldElement.Invert(z_2);
        BatchedFieldElement result = BatchedFieldElement.Multiply(x_2, z_2_inv);

        // Распаковка результатов
        result.ToBytes4(out0, out1, out2, out3);
    }

    private static void Clamp(Span<byte> k)
    {
        k[0] &= 248;
        k[31] &= 127;
        k[31] |= 64;
    }
}