using Core.Math;

namespace Core;

public static class X25519
{
    /// <summary>
    /// Вычисляет X25519(k, u).
    /// </summary>
    /// <param name="scalar">32 байта секретного ключа (скаляр)</param>
    /// <param name="uCoordinate">32 байта публичного ключа (U-координата)</param>
    /// <returns>32 байта общего секрета (Shared Secret)</returns>
    public static byte[] Multiply(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uCoordinate)
    {
        // Копируем скаляр, чтобы применить к нему Clamping (по RFC 7748)
        Span<byte> k = stackalloc byte[32];
        scalar.CopyTo(k);
        k[0] &= 248;
        k[31] &= 127;
        k[31] |= 64;

        // Декодируем U-координату
        FieldElement x_1 = FieldElement.FromBytes(uCoordinate);
        
        // Инициализация переменных лестницы
        FieldElement x_2 = FieldElement.One;
        FieldElement z_2 = FieldElement.Zero;
        FieldElement x_3 = x_1;
        FieldElement z_3 = FieldElement.One;
        
        int swap = 0;

        // Лестница Монтгомери (идем от 254 бита до 0)
        for (int t = 254; t >= 0; t--)
        {
            // Извлекаем t-й бит скаляра
            int k_t = (k[t / 8] >> (t % 8)) & 1;
            
            swap ^= k_t;
            FieldElement.CSwap(ref x_2, ref x_3, swap);
            FieldElement.CSwap(ref z_2, ref z_3, swap);
            swap = k_t;

            // Арифметика Монтгомери (строго по формулам RFC)
            FieldElement A = FieldElement.Add(x_2, z_2);
            FieldElement AA = FieldElement.Square(A);
            FieldElement B = FieldElement.Sub(x_2, z_2);
            FieldElement BB = FieldElement.Square(B);
            
            FieldElement E = FieldElement.Sub(AA, BB);
            
            FieldElement C = FieldElement.Add(x_3, z_3);
            FieldElement D = FieldElement.Sub(x_3, z_3);
            
            FieldElement DA = FieldElement.Multiply(D, A);
            FieldElement CB = FieldElement.Multiply(C, B);
            
            FieldElement DA_plus_CB = FieldElement.Add(DA, CB);
            x_3 = FieldElement.Square(DA_plus_CB);
            
            FieldElement DA_minus_CB = FieldElement.Sub(DA, CB);
            FieldElement DA_minus_CB_sq = FieldElement.Square(DA_minus_CB);
            z_3 = FieldElement.Multiply(x_1, DA_minus_CB_sq);
            
            x_2 = FieldElement.Multiply(AA, BB);
            
            FieldElement a24_E = FieldElement.Multiply(FieldElement.A24, E);
            FieldElement AA_plus_a24_E = FieldElement.Add(AA, a24_E);
            z_2 = FieldElement.Multiply(E, AA_plus_a24_E);
        }

        // Финальный фиктивный swap, чтобы предотвратить утечку последнего бита
        FieldElement.CSwap(ref x_2, ref x_3, swap);
        FieldElement.CSwap(ref z_2, ref z_3, swap);

        // Вычисление итогового результата: x_2 * z_2^(p-2)
        FieldElement z_2_inv = FieldElement.Invert(z_2);
        FieldElement result = FieldElement.Multiply(x_2, z_2_inv);

        // Сериализация в байты
        byte[] output = new byte[32];
        result.ToBytes(output);
        
        return output;
    }
}