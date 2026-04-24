namespace Core.X25519;

public static class X25519DiffieHellman
{
    // Базовая точка X25519 (u = 9). 
    // Копируем 4 раза по 32 байта (итого 128 байт) для векторной обработки батча.
    private static ReadOnlySpan<byte> BasePoint4 => new byte[128]
    {
        9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    };
    
    /// <summary>
    /// Генерирует 4 публичных ключа из 4 приватных ключей.
    /// </summary>
    /// <param name="secretKeys128">4 секретных ключа по 32 байта (длина 128).</param>
    /// <param name="publicKeys128">4 сгенерированных публичных ключа по 32 байта.</param>
    public static void GeneratePublicKeysBatch(ReadOnlySpan<byte> secretKeys128, Span<byte> publicKeys128)
    {
        // В вашей функции Multiply4 ключи уже "клемпятся" (обрезаются нужные биты),
        // поэтому мы можем безопасно передать их прямо туда с базовой точкой.
        X25519Batch.Multiply4(secretKeys128, BasePoint4, publicKeys128);
    }
    
    /// <summary>
    /// Вычисляет общий секрет (Shared Secret) для 4 каналов одновременно.
    /// </summary>
    /// <param name="mySecretKeys128">Ваши 4 приватных ключа.</param>
    /// <param name="peerPublicKeys128">4 публичных ключа другой стороны.</param>
    /// <param name="sharedSecrets128">Итоговые общие секреты, готовые к хешированию (HKDF).</param>
    public static void DeriveSharedSecretsBatch(
        ReadOnlySpan<byte> mySecretKeys128,
        ReadOnlySpan<byte> peerPublicKeys128,
        Span<byte> sharedSecrets128)
    {
        // Shared Secret = (MyPrivateKey * PeerPublicKey)
        X25519Batch.Multiply4(mySecretKeys128, peerPublicKeys128, sharedSecrets128);
    }
}