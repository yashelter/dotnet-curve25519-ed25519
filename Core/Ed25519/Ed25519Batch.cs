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
        FieldElement4.Multiply(ref C, in C, in Ed25519Constants.D2);

        FieldElement4.Multiply(ref D, in p1.Z, in p2.Z);
        FieldElement4.Add(ref D, in D, in D);

        FieldElement4.Apm(ref H, ref E, in B, in A);
        FieldElement4.Apm(ref G, ref F, in D, in C);

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
        FieldElement4.Add(ref C, in C, in C);

        FieldElement4.Apm(ref H, ref G, in A, in B);

        FieldElement4.Add(ref X1_plus_Y1, in p.X, in p.Y);
        FieldElement4.Square(ref E, in X1_plus_Y1);
        FieldElement4.Sub(ref E, in H, in E);

        FieldElement4.Add(ref F, in C, in G);

        FieldElement4.Multiply(ref result.X, in E, in F);
        FieldElement4.Multiply(ref result.Y, in G, in H);
        FieldElement4.Multiply(ref result.T, in E, in H);
        FieldElement4.Multiply(ref result.Z, in F, in G);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Multiply4(ReadOnlySpan<byte> scalars, in PointExt4 basePoints, out PointExt4 result)
    {
        // Предвычисляем 16 точек для нашего окна (0*P, 1*P, ..., 15*P)
        Span<PointExt4> table = stackalloc PointExt4[16];
        table[0] = PointExt4.Zero;
        table[1] = basePoints;
        for (int i = 2; i < 16; i++)
        {
            Add(ref table[i], in table[i - 1], in basePoints);
        }

        PointExt4 R = PointExt4.Zero;
        PointExt4 tempAdd = default;

        //  Идем по 4 бита (256 бит / 4 = 64 итерации). От старших к младшим
        for (int i = 63; i >= 0; i--)
        {
            // Пропускаем 4 Double на самой первой итерации, так как R == Zero
            if (i != 63)
            {
                Double(ref R, in R);
                Double(ref R, in R);
                Double(ref R, in R);
                Double(ref R, in R);
            }

            Vector256<ulong> indices = Get4BitWindow(scalars, i);
            
            SelectPoint(ref tempAdd, table, indices);
            Add(ref R, in R, in tempAdd);
        }

        result = R;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Get4BitWindow(ReadOnlySpan<byte> scalars, int chunkIndex)
    {
        int byteIdx = chunkIndex >> 1; // Делим на 2
        int shift = (chunkIndex & 1) << 2; // Умножаем остаток на 4 (0 или 4)

        // Достаем значения 0..15 для каждого из 4 скаляров
        ulong idx0 = (ulong)((scalars[byteIdx] >> shift) & 0x0F);
        ulong idx1 = (ulong)((scalars[32 + byteIdx] >> shift) & 0x0F);
        ulong idx2 = (ulong)((scalars[64 + byteIdx] >> shift) & 0x0F);
        ulong idx3 = (ulong)((scalars[96 + byteIdx] >> shift) & 0x0F);

        return Vector256.Create(idx0, idx1, idx2, idx3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SelectPoint(ref PointExt4 dest, ReadOnlySpan<PointExt4> table, Vector256<ulong> indices)
    {
        dest = table[0];
        
        // Переписываем каналы нужными точками, если их индекс совпал
        for (ulong j = 1; j < 16; j++)
        {
            var matchMask = Vector256.Equals(indices, Vector256.Create(j));
            PointExt4.CSelect(ref dest, in dest, in table[(int)j], matchMask);
        }
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