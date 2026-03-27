using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


public static class X25519Batch
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Multiply4(ReadOnlySpan<byte> scalars, ReadOnlySpan<byte> uCoordinates, Span<byte> output)
    {
        Span<byte> k0 = stackalloc byte[32]; Span<byte> k1 = stackalloc byte[32];
        Span<byte> k2 = stackalloc byte[32]; Span<byte> k3 = stackalloc byte[32];

        scalars.Slice(0, 32).CopyTo(k0); scalars.Slice(32, 32).CopyTo(k1);
        scalars.Slice(64, 32).CopyTo(k2); scalars.Slice(96, 32).CopyTo(k3);

        k0[0] &= 248; k0[31] &= 127; k0[31] |= 64;
        k1[0] &= 248; k1[31] &= 127; k1[31] |= 64;
        k2[0] &= 248; k2[31] &= 127; k2[31] |= 64;
        k3[0] &= 248; k3[31] &= 127; k3[31] |= 64;

        FieldElement4 x_1 = FieldElement4.FromBytes(uCoordinates);
        FieldElement4 x_2 = FieldElement4.One, z_2 = FieldElement4.Zero;
        FieldElement4 x_3 = x_1, z_3 = FieldElement4.One;

        FieldElement4 A = default, AA = default, B = default, BB = default;
        FieldElement4 E = default, C = default, D = default;
        FieldElement4 DA = default, CB = default, DA_plus_CB = default, DA_minus_CB = default;
        FieldElement4 a24_E = default, AA_plus_a24_E = default;

        Vector256<ulong> swap = Vector256<ulong>.Zero;

        for (int t = 254; t >= 0; t--)
        {
            int bit = t & 7;
            int byteIndex = t >> 3;

            // Извлекаем нужные биты без ветвлений и условной логики массива
            Vector256<ulong> k_t = Vector256.Create(
                (ulong)((k0[byteIndex] >> bit) & 1),
                (ulong)((k1[byteIndex] >> bit) & 1),
                (ulong)((k2[byteIndex] >> bit) & 1),
                (ulong)((k3[byteIndex] >> bit) & 1));

            swap = Avx2.Xor(swap, k_t);
            FieldElement4.CSwap(ref x_2, ref x_3, swap);
            FieldElement4.CSwap(ref z_2, ref z_3, swap);
            swap = k_t;

            FieldElement4.Apm(ref A, ref B, in x_2, in z_2);
            FieldElement4.Square(ref AA, in A);
            FieldElement4.Square(ref BB, in B);
            FieldElement4.Sub(ref E, in AA, in BB);

            FieldElement4.Apm(ref C, ref D, in x_3, in z_3);
            FieldElement4.Multiply(ref DA, in D, in A);
            FieldElement4.Multiply(ref CB, in C, in B);
            FieldElement4.Apm(ref DA_plus_CB, ref DA_minus_CB, in DA, in CB);

            FieldElement4.Square(ref x_3, in DA_plus_CB);
            FieldElement4.Square(ref DA_minus_CB, in DA_minus_CB);
            FieldElement4.Multiply(ref z_3, in x_1, in DA_minus_CB);

            FieldElement4.Multiply(ref x_2, in AA, in BB);
            FieldElement4.Multiply(ref a24_E, in FieldElement4.A24, in E);
            FieldElement4.Add(ref AA_plus_a24_E, in AA, in a24_E);
            FieldElement4.Multiply(ref z_2, in E, in AA_plus_a24_E);
        }

        FieldElement4.CSwap(ref x_2, ref x_3, swap);
        FieldElement4.CSwap(ref z_2, ref z_3, swap);

        FieldElement4 z_2_inv = default;
        FieldElement4.Invert(ref z_2_inv, in z_2);

        FieldElement4 result = default;
        FieldElement4.Multiply(ref result, in x_2, in z_2_inv);

        // Распаковываем (происходит 1 раз в конце)
        result.ToBytes(output);
    }
}