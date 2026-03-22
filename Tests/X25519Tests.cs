using System.Diagnostics;
using Core;
using Core.Math;
using Xunit.Abstractions;

namespace Tests;
using BcX25519 = Org.BouncyCastle.Math.EC.Rfc7748.X25519;

public class X25519Tests
{
    private readonly ITestOutputHelper _output;

    // Инжектим ITestOutputHelper для вывода текста в лог тестов
    public X25519Tests(ITestOutputHelper output)
    {
        _output = output;
    }

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
    
    // --- 1. ТЕСТЫ НА КОРРЕКТНОСТЬ (SCALAR) 
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

    // --- 2. ТЕСТЫ НА КОРРЕКТНОСТЬ (BATCHED SIMD) ---

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

        // Выделяем память под результаты 4 параллельных операций
        byte[] out0 = new byte[32];
        byte[] out1 = new byte[32];
        byte[] out2 = new byte[32];
        byte[] out3 = new byte[32];

        // Запускаем смешанный батч: Вектор1, Вектор2, Вектор1, Вектор2
        BatchedX25519.Multiply4(
            k1, u1, out0,
            k2, u2, out1,
            k1, u1, out2,
            k2, u2, out3
        );

        // Проверяем, что все 4 линии отработали независимо и корректно
        Assert.Equal(expected1, BytesToHex(out0));
        Assert.Equal(expected2, BytesToHex(out1));
        Assert.Equal(expected1, BytesToHex(out2));
        Assert.Equal(expected2, BytesToHex(out3));
    }

    //  СРАВНИТЕЛЬНЫЙ БЕНЧМАРК ПРОИЗВОДИТЕЛЬНОСТИ ---

    [Theory]
    [InlineData(10000)]
    public void PerformanceBenchmark_X25519(int iterations)
    {
        long before = GC.GetTotalAllocatedBytes();
            
        // 1. Подготовка общих данных (используем Вектор 1)
        byte[] scalar = HexToBytes("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        byte[] uCoord = HexToBytes("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
        
        byte[] bcOutput = new byte[32];
        
        // Буферы для Batched (чтобы не аллоцировать память внутри цикла)
        byte[] out0 = new byte[32], out1 = new byte[32], out2 = new byte[32], out3 = new byte[32];

        _output.WriteLine($"--- Запуск бенчмарка: {iterations:N0} итераций ---");

        // --- Тест 1: BouncyCastle ---
        // Прогрев JIT
        BcX25519.ScalarMult(scalar, 0, uCoord, 0, bcOutput, 0); 
        
        Stopwatch sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            BcX25519.ScalarMult(scalar, 0, uCoord, 0, bcOutput, 0);
        }
        sw1.Stop();
        _output.WriteLine($"[1] BouncyCastle (C# Эталон): {sw1.ElapsedMilliseconds} ms");
        
        long after = GC.GetTotalAllocatedBytes();
        _output.WriteLine($"Выделено памяти: {(after - before) / 1024 } kбайт");
        before = GC.GetTotalAllocatedBytes();

        // --- Тест 2: Твой Скалярный X25519 ---
        // Прогрев JIT
        X25519.Multiply(scalar, uCoord, bcOutput);
        
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            X25519.Multiply(scalar, uCoord, bcOutput);
        }
        sw2.Stop();
        _output.WriteLine($"[2] Custom Scalar X25519:   {sw2.ElapsedMilliseconds} ms " +
                          $"(В {sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F2}x раз быстрее BC)");
        
        after = GC.GetTotalAllocatedBytes();
        _output.WriteLine($"Выделено памяти: {(after - before) / 1024 } kбайт");
        _output.WriteLine("-------------------------------------------------");
    }
}