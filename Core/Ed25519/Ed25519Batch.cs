using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Core.Base;

namespace Core.Ed25519;

public static class Ed25519Batch
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Add(ref PointExt4 result, in PointExt4 p1, in PointExt4 p2)
    {
        FieldElement4 A = default, B = default, C = default, D = default;
        FieldElement4 E = default, F = default, G = default, H = default;
        FieldElement4 Y1_minus_X1 = default, Y1_plus_X1 = default;
        FieldElement4 Y2_minus_X2 = default, Y2_plus_X2 = default;

        FieldElement4.Apm(ref Y1_plus_X1, ref Y1_minus_X1, in p1.Y, in p1.X);
        FieldElement4.Apm(ref Y2_plus_X2, ref Y2_minus_X2, in p2.Y, in p2.X);

        FieldElement4.Multiply(ref A, in Y1_minus_X1, in Y2_minus_X2);
        FieldElement4.Multiply(ref B, in Y1_plus_X1, in Y2_plus_X2);
        
        FieldElement4.Multiply(ref C, in p1.T, in p2.T);
        FieldElement4.Multiply(ref C, in C, in Ed25519Constants.D2); // C = T1 * T2 * 2d

        FieldElement4.Multiply(ref D, in p1.Z, in p2.Z);
        FieldElement4.Add(ref D, in D, in D); // D = 2 * Z1 * Z2

        FieldElement4.Apm(ref H, ref E, in B, in A); // H = B + A, E = B - A
        FieldElement4.Apm(ref G, ref F, in D, in C); // G = D + C, F = D - C

        FieldElement4.Multiply(ref result.X, in E, in F);
        FieldElement4.Multiply(ref result.Y, in G, in H);
        FieldElement4.Multiply(ref result.T, in E, in H);
        FieldElement4.Multiply(ref result.Z, in F, in G);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Double(ref PointExt4 result, in PointExt4 p)
    {
        FieldElement4 A = default, B = default, C = default;
        FieldElement4 E = default, F = default, G = default, H = default;
        FieldElement4 X1_plus_Y1 = default;

        FieldElement4.Square(ref A, in p.X);
        FieldElement4.Square(ref B, in p.Y);

        FieldElement4.Square(ref C, in p.Z);
        FieldElement4.Add(ref C, in C, in C); // C = 2 * Z1^2

        FieldElement4.Apm(ref H, ref G, in A, in B); // H = A + B, G = A - B

        FieldElement4.Add(ref X1_plus_Y1, in p.X, in p.Y);
        FieldElement4.Square(ref E, in X1_plus_Y1);
        FieldElement4.Sub(ref E, in H, in E); // E = H - (X1 + Y1)^2 = -2X1Y1

        FieldElement4.Add(ref F, in C, in G); // F = C + A - B = 2Z1^2 - (Y1^2 - X1^2)

        FieldElement4.Multiply(ref result.X, in E, in F);
        FieldElement4.Multiply(ref result.Y, in G, in H);
        FieldElement4.Multiply(ref result.T, in E, in H);
        FieldElement4.Multiply(ref result.Z, in F, in G);
    }

    
    // --- Умножение скаляров ---
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Multiply4(ReadOnlySpan<byte> scalars, in PointExt4 basePoints, out PointExt4 result)
    {
        // Извлекаем маски битов ПЕРЕД циклом, чтобы не создавать Pipeline Stalls внутри горячего лупа
        Span<Vector256<ulong>> k_masks = stackalloc Vector256<ulong>[256];
        ReadOnlySpan<byte> k0 = scalars.Slice(0, 32);
        ReadOnlySpan<byte> k1 = scalars.Slice(32, 32);
        ReadOnlySpan<byte> k2 = scalars.Slice(64, 32);
        ReadOnlySpan<byte> k3 = scalars.Slice(96, 32);

        for (int t = 0; t < 256; t++)
        {
            int bit = t & 7;
            int byteIndex = t >> 3;
            
            // Если бит равен 1, маска 0xFF..FF, иначе 0
            k_masks[t] = Vector256.Create(
                (ulong)((k0[byteIndex] >> bit) & 1) > 0 ? 0xFFFFFFFFFFFFFFFFUL : 0,
                (ulong)((k1[byteIndex] >> bit) & 1) > 0 ? 0xFFFFFFFFFFFFFFFFUL : 0,
                (ulong)((k2[byteIndex] >> bit) & 1) > 0 ? 0xFFFFFFFFFFFFFFFFUL : 0,
                (ulong)((k3[byteIndex] >> bit) & 1) > 0 ? 0xFFFFFFFFFFFFFFFFUL : 0);
        }

        PointExt4 R = PointExt4.Zero;
        PointExt4 tempAdd = default;

        // Константное время: Итерация Double-And-Add (без ветвлений)
        for (int t = 255; t >= 0; t--)
        {
            Double(ref R, in R);
            Add(ref tempAdd, in R, in basePoints);
            PointExt4.CSelect(ref R, in R, in tempAdd, k_masks[t]);
        }

        result = R;
    }

    
    // --- Конвертация из Аффинных и обратно ---
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void FromAffine(out PointExt4 result, in FieldElement4 x, in FieldElement4 y)
    {
        result.T = FieldElement4.Zero;
        result.X = x;
        result.Y = y;
        result.Z = FieldElement4.One;
        FieldElement4.Multiply(ref result.T, in x, in y);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ToAffine(in PointExt4 p, out FieldElement4 x, out FieldElement4 y)
    {
        x = FieldElement4.Zero;
        y = FieldElement4.Zero;
        
        FieldElement4 zInv = default;
        FieldElement4.Invert(ref zInv, in p.Z);
        
        FieldElement4.Multiply(ref x, in p.X, in zInv);
        FieldElement4.Multiply(ref y, in p.Y, in zInv);
    }
}