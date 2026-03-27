using System.Diagnostics;
using Core;
using Core.X25519;
using Xunit.Abstractions;

namespace Tests;
using BcX25519 = Org.BouncyCastle.Math.EC.Rfc7748.X25519;

public class X25519Tests(ITestOutputHelper output)
{
    // Вспомогательный метод перевода hex-строки в байты
    private static byte[] HexToBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
    
    // Вспомогательный метод перевода байтов в hex-строку
    private static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
    
    
    // --- ТЕСТЫ НА КОРРЕКТНОСТЬ
    [Fact]
    public void ScalarX25519_TestVector1_FromRFC7748()
    {
        string scalarHex = "a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4";
        string uCoordinateHex = "e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c";
        string expectedOutputHex = "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552";
        
        byte[] result = X25519.Multiply(HexToBytes(scalarHex), HexToBytes(uCoordinateHex));
        Assert.Equal(expectedOutputHex, BytesToHex(result));
    }

    [Fact]
    public void ScalarX25519_TestVector2_FromRFC7748()
    {
        string scalarHex = "4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d";
        string uCoordinateHex = "e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493";
        string expectedOutputHex = "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957";
        
        byte[] result = X25519.Multiply(HexToBytes(scalarHex), HexToBytes(uCoordinateHex));
        Assert.Equal(expectedOutputHex, BytesToHex(result));
    }


    [Fact]
    public void BatchedX25519_Correctness_MixedVectors()
    {
        // Подготавливаем векторы из RFC 7748
        byte[] k1 = HexToBytes("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        byte[] u1 = HexToBytes("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
        string expected1 = "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552";

        byte[] k2 = HexToBytes("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d");
        byte[] u2 = HexToBytes("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493");
        string expected2 = "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957";
        
        
        // Упаковываем 4 скаляра подряд: [k1 | k2 | k1 | k2]
        byte[] scalars = new byte[128];
        k1.CopyTo(scalars.AsSpan(  0));
        k2.CopyTo(scalars.AsSpan( 32));
        k1.CopyTo(scalars.AsSpan( 64));
        k2.CopyTo(scalars.AsSpan( 96));
        
        // Упаковываем 4 u-координаты подряд: [u1 | u2 | u1 | u2]
        byte[] uCoords = new byte[128];
        u1.CopyTo(uCoords.AsSpan(  0));
        u2.CopyTo(uCoords.AsSpan( 32));
        u1.CopyTo(uCoords.AsSpan( 64));
        u2.CopyTo(uCoords.AsSpan( 96));
        
        byte[] output = new byte[128];
        
        X25519Batch.Multiply4(scalars, uCoords, output);
        
        // Распаковываем результаты
        string out0 = BytesToHex(output.AsSpan(  0, 32).ToArray());
        string out1 = BytesToHex(output.AsSpan( 32, 32).ToArray());
        string out2 = BytesToHex(output.AsSpan( 64, 32).ToArray());
        string out3 = BytesToHex(output.AsSpan( 96, 32).ToArray());
        
        
        Assert.Equal((out0), (out2));
        Assert.Equal((out1), (out3));

        // Проверяем, что все 4 линии отработали независимо и корректно
        Assert.Equal(expected1, (out0));
        Assert.Equal(expected2, (out1));
        Assert.Equal(expected1, (out2));
        Assert.Equal(expected2, (out3));
    }

    //  СРАВНИТЕЛЬНЫЙ БЕНЧМАРК ПРОИЗВОДИТЕЛЬНОСТИ ---

    [Theory]
    [InlineData(100000)]
    [InlineData(10000)]
    [InlineData(1000)]
    [InlineData(100)]
    [InlineData(16)]
    public void PerformanceBenchmark_X25519(int iterations)
    {
            
        // 1. Подготовка общих данных (используем Вектор 1)
        byte[] scalar = HexToBytes("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        byte[] uCoord = HexToBytes("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
        
        byte[] bcOutput = new byte[32];
        
        // Буферы для Batched (чтобы не аллоцировать память внутри цикла)
        byte[] out0 = new byte[32], out1 = new byte[32], out2 = new byte[32], out3 = new byte[32];

        output.WriteLine($"--- Запуск бенчмарка: {iterations:N0} итераций ---");

        // --- Тест 1: BouncyCastle ---
        // Прогрев JIT
        BcX25519.ScalarMult(scalar, 0, uCoord, 0, bcOutput, 0);
        long before = GC.GetTotalAllocatedBytes();

        Stopwatch sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            BcX25519.ScalarMult(scalar, 0, uCoord, 0, bcOutput, 0);
        }
        sw1.Stop();
        output.WriteLine($"[1] BouncyCastle (C# Эталон): {sw1.ElapsedMilliseconds} ms");
        
        long after = GC.GetTotalAllocatedBytes();
        output.WriteLine($"Выделено памяти: {(after - before) / 1024 } kбайт");

        // --- Тест 2: Скалярный X25519 ---
        // no --> ignore
        // --- Тест 3: Batched X25519 ---

        before = GC.GetTotalAllocatedBytes();
        
        // Подготавливаем векторы из RFC 7748
        byte[] k1 = HexToBytes("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        byte[] u1 = HexToBytes("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
        string expected1 = "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552";
        
        byte[] k2 = HexToBytes("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d");
        byte[] u2 = HexToBytes("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493");
        string expected2 = "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957";
        
        
        // Упаковываем 4 скаляра подряд: [k1 | k2 | k1 | k2]
        byte[] scalars = new byte[128];
        k1.CopyTo(scalars.AsSpan(  0));
        k2.CopyTo(scalars.AsSpan( 32));
        k1.CopyTo(scalars.AsSpan( 64));
        k2.CopyTo(scalars.AsSpan( 96));
        
        // Упаковываем 4 u-координаты подряд: [u1 | u2 | u1 | u2]
        byte[] uCoords = new byte[128];
        u1.CopyTo(uCoords.AsSpan(  0));
        u2.CopyTo(uCoords.AsSpan( 32));
        u1.CopyTo(uCoords.AsSpan( 64));
        u2.CopyTo(uCoords.AsSpan( 96));
        
        byte[] output1 = new byte[128];
        X25519Batch.Multiply4(scalars, uCoords, output1);
        
        var sw3 = Stopwatch.StartNew();
        for (int i = 0; i < iterations / 4; i++)
        {
            X25519Batch.Multiply4(scalars, uCoords, output1);
        }
        sw3.Stop();
        output.WriteLine($"[2] Batch  Scalar X25519:   {sw3.ElapsedMilliseconds} ms " +
                          $"(В {sw1.ElapsedMilliseconds / (double)sw3.ElapsedMilliseconds:F2}x раз быстрее BC)");
        
        after = GC.GetTotalAllocatedBytes();
        output.WriteLine($"Выделено памяти: {(after - before) / 1024 } kбайт");
        output.WriteLine("-------------------------------------------------");
    }
}