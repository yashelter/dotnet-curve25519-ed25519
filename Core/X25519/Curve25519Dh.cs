using System.Security.Cryptography;

namespace Core.X25519;


/// <summary>
/// Реализация протокола обмена ключами Elliptic-Curve Diffie-Hellman (ECDH) поверх Curve25519.
/// </summary>
public static class Curve25519Dh
{
    // Базовая точка для Curve25519 (согласно RFC 7748, U(P) = 9).
    // Представляется как 32 байта в little-endian формате.
    private static readonly byte[] _basePoint = "\t\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"u8.ToArray();

    /// <summary>
    /// Генерирует 32 байта криптографически стойкой случайности для приватного ключа.
    /// </summary>
    /// <returns>32-байтовый массив приватного ключа.</returns>
    public static byte[] GeneratePrivateKey()
    {
        byte[] privateKey = new byte[32];
        // Используем криптографически стойкий генератор случайных чисел (CSPRNG)
        RandomNumberGenerator.Fill(privateKey);
        
        // Примечание: "Clamping" (зануление/установка битов ключа) по RFC 7748 
        // происходит внутри самой математической функции X25519.Multiply.
        return privateKey;
    }

    /// <summary>
    /// Вычисляет публичный ключ (U-координату) на основе приватного ключа.
    /// PublicKey = X25519(PrivateKey, BasePoint)
    /// </summary>
    /// <param name="privateKey">32 байта приватного ключа.</param>
    /// <returns>32 байта публичного ключа.</returns>
    public static byte[] GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be exactly 32 bytes long.");
        }
        
        return X25519.Multiply(privateKey, _basePoint);
    }

    /// <summary>
    /// Вычисляет общий секрет (Shared Secret), используя свой приватный ключ и чужой публичный ключ.
    /// SharedSecret = X25519(MyPrivateKey, TheirPublicKey)
    /// </summary>
    /// <param name="myPrivateKey">Свой приватный ключ (32 байта).</param>
    /// <param name="theirPublicKey">Чужой публичный ключ (32 байта).</param>
    /// <returns>32 байта общего секрета.</returns>
    public static byte[] ComputeSharedSecret(ReadOnlySpan<byte> myPrivateKey, ReadOnlySpan<byte> theirPublicKey)
    {
        if (myPrivateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be exactly 32 bytes long.");
        }
        
        if (theirPublicKey.Length != 32)
        {
            throw new ArgumentException("Public key must be exactly 32 bytes long.");
        }
        
        return X25519.Multiply(myPrivateKey, theirPublicKey);
    }
}