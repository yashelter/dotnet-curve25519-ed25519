using Core;

namespace Tests;

public class Curve25519DhTests
{
    // Вспомогательный метод: из Hex в массив байтов
    private static byte[] HexToBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        
        return bytes;
    }
    
    // Вспомогательный метод: из массива байтов в Hex
    private static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
    
    /// <summary>
    /// Проверка жизненного цикла: генерация ключей и успешное вычисление одинакового секрета.
    /// </summary>
    [Fact]
    public void DiffieHellman_AliceAndBob_ShouldGenerateSameSharedSecret()
    {
        // 1. Алиса генерирует свои ключи
        byte[] alicePrivateKey = Curve25519Dh.GeneratePrivateKey();
        byte[] alicePublicKey = Curve25519Dh.GetPublicKey(alicePrivateKey);
        
        // 2. Боб генерирует свои ключи
        byte[] bobPrivateKey = Curve25519Dh.GeneratePrivateKey();
        byte[] bobPublicKey = Curve25519Dh.GetPublicKey(bobPrivateKey);
        
        // Убедимся, что ключи имеют правильную длину
        Assert.Equal(32, alicePublicKey.Length);
        Assert.Equal(32, bobPublicKey.Length);
        
        // -- ОБМЕН ПУБЛИЧНЫМИ КЛЮЧАМИ --
        // (Алиса отправляет alicePublicKey Бобу, Боб отправляет bobPublicKey Алисе по открытому каналу)
        
        // 3. Алиса вычисляет общий секрет, используя свой приватный и публичный Боба
        byte[] aliceSharedSecret = Curve25519Dh.ComputeSharedSecret(alicePrivateKey, bobPublicKey);
        
        // 4. Боб вычисляет общий секрет, используя свой приватный и публичный Алисы
        byte[] bobSharedSecret = Curve25519Dh.ComputeSharedSecret(bobPrivateKey, alicePublicKey);
        
        // 5. Проверка: Секреты должны совпасть!
        Assert.True(aliceSharedSecret.SequenceEqual(bobSharedSecret), "Shared secrets do not match!");
        
        // Секрет не должен быть пустым (все нули)
        Assert.False(aliceSharedSecret.All(b => b == 0), "Shared secret is all zeros!");
    }
    
    /// <summary>
    /// Тест на основе официальных векторов из RFC 7748 (Раздел 6.1. Curve25519).
    /// Доказывает правильность работы алгоритма на базовой точке U = 9.
    /// </summary>
    [Fact]
    public void DiffieHellman_RFC7748_Section6_1_Vectors_ShouldMatch()
    {
        // Arrange (Данные из RFC 7748, раздел 6.1)
        string alicePrivateKeyHex = "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a";
        string expectedAlicePublicKeyHex = "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a";
        
        string bobPrivateKeyHex = "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb";
        string expectedBobPublicKeyHex = "de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f";
        
        string expectedSharedSecretHex = "4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742";
        
        byte[] alicePrivateKey = HexToBytes(alicePrivateKeyHex);
        byte[] bobPrivateKey = HexToBytes(bobPrivateKeyHex);
        
        // Act & Assert 1: Проверяем публичный ключ Алисы (умножение на базовую точку 9)
        byte[] alicePublicKey = Curve25519Dh.GetPublicKey(alicePrivateKey);
        Assert.Equal(expectedAlicePublicKeyHex, BytesToHex(alicePublicKey));
        
        // Act & Assert 2: Проверяем публичный ключ Боба (умножение на базовую точку 9)
        byte[] bobPublicKey = Curve25519Dh.GetPublicKey(bobPrivateKey);
        Assert.Equal(expectedBobPublicKeyHex, BytesToHex(bobPublicKey));
        
        // Act & Assert 3: Алиса вычисляет секрет
        byte[] aliceSharedSecret = Curve25519Dh.ComputeSharedSecret(alicePrivateKey, bobPublicKey);
        Assert.Equal(expectedSharedSecretHex, BytesToHex(aliceSharedSecret));
        
        // Act & Assert 4: Боб вычисляет секрет
        byte[] bobSharedSecret = Curve25519Dh.ComputeSharedSecret(bobPrivateKey, alicePublicKey);
        
        Assert.True(aliceSharedSecret.SequenceEqual(bobSharedSecret), "Shared secrets do not match!");
        Assert.False(aliceSharedSecret.All(b => b == 0), "Shared secret is all zeros!");
    }
}