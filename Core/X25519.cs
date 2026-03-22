using Core.Math;

namespace Core;

public static class X25519
{
    /// <summary>
    /// Вычисляет X25519(k, u).
    /// </summary>
    /// <param name="scalar">32 байта секретного ключа (скаляр)</param>
    /// <param name="uCoordinate">32 байта публичного ключа (U-координата)</param>
    /// <param name="output">Выходной массив</param>
    /// <returns>32 байта общего секрета (Shared Secret)</returns>
    public static void Multiply(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uCoordinate, Span<byte> output)
    {
        Span<byte> k = stackalloc byte[32];
        scalar.CopyTo(k);
        k[0] &= 248;
        k[31] &= 127;
        k[31] |= 64;

        FieldElement x_1 = FieldElement.FromBytes(uCoordinate);
        
        FieldElement x_2 = FieldElement.One;
        FieldElement z_2 = FieldElement.Zero;
        FieldElement x_3 = x_1;
        FieldElement z_3 = FieldElement.One;
        
        FieldElement A = default, AA = default, B = default, BB = default;
        FieldElement E = default, C = default, D = default;
        FieldElement DA = default, CB = default;
        FieldElement DA_plus_CB = default, DA_minus_CB = default;
        FieldElement a24_E = default, AA_plus_a24_E = default;
        
        int swap = 0;

        for (int t = 254; t >= 0; t--)
        {
            int k_t = (k[t >> 3] >> (t & 7)) & 1; // Оптимизировано деление
            
            swap ^= k_t;
            FieldElement.CSwap(ref x_2, ref x_3, swap);
            FieldElement.CSwap(ref z_2, ref z_3, swap);
            swap = k_t;

            FieldElement.Apm(ref A, ref B, in x_2, in z_2);
            FieldElement.Square(ref AA, in A);
            FieldElement.Square(ref BB, in B);
            FieldElement.Sub(ref E, in AA, in BB);
            FieldElement.Apm(ref C, ref D, in x_3, in z_3);
            FieldElement.Multiply(ref DA, in D, in A);
            FieldElement.Multiply(ref CB, in C, in B);
            FieldElement.Apm(ref DA_plus_CB, ref DA_minus_CB, in DA, in CB);
            FieldElement.Square(ref x_3, in DA_plus_CB);
            FieldElement.Square(ref DA_minus_CB, in DA_minus_CB); 
            FieldElement.Multiply(ref z_3, in x_1, in DA_minus_CB);
            FieldElement.Multiply(ref x_2, in AA, in BB);
            FieldElement.Multiply(ref a24_E, in FieldElement.A24, in E);
            FieldElement.Add(ref AA_plus_a24_E, in AA, in a24_E);
            FieldElement.Multiply(ref z_2, in E, in AA_plus_a24_E);
        }

        FieldElement.CSwap(ref x_2, ref x_3, swap);
        FieldElement.CSwap(ref z_2, ref z_3, swap);

        FieldElement z_2_inv = FieldElement.Invert(in z_2);
        
        FieldElement result = default;
        FieldElement.Multiply(ref result, in x_2, in z_2_inv);

        // Запись прямо в переданный буфер
        result.ToBytes(output);
    }
    
    public static byte[] Multiply(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uCoordinate)
    {
        byte[] output = new byte[32]; // A bit slower because of this
        Multiply(scalar, uCoordinate, output);
        return output;
    }
}