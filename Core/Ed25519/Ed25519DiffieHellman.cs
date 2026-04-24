using System.Security.Cryptography;

namespace Core.Ed25519;

public static class Ed25519DiffieHellman
{
    /// <summary>
    /// Генерация 4 публичных ключей одновременно
    /// </summary>
    public static void GeneratePublicKeysBatch(ReadOnlySpan<byte> secretKeys128, Span<byte> publicKeys128)
    {
        Span<byte> clampedScalars = stackalloc byte[128];
        
        // По стандарту RFC 8032: скаляр получается через SHA-512 от Secret Key
        for(int i = 0; i < 4; i++)
        {
            ReadOnlySpan<byte> sk = secretKeys128.Slice(i * 32, 32);
            Span<byte> hash = stackalloc byte[64];
            SHA512.HashData(sk, hash);
            
            Span<byte> scalar = clampedScalars.Slice(i * 32, 32);
            hash.Slice(0, 32).CopyTo(scalar);

            // Clamping (очистка битов для защиты от малых подгрупп)
            scalar[0] &= 248; 
            scalar[31] &= 127; 
            scalar[31] |= 64;
        }

        Ed25519Batch.Multiply4(clampedScalars, in PointExt4.BasePoint, out var R);
        R.EncodeToBytes(publicKeys128);
    }

    /// <summary>
    /// Вычисление Shared Secret (ECDH). peerPublicPoints - предварительно декодированные точки партнеров.
    /// </summary>
    public static void DeriveSharedSecretsBatch(
        ReadOnlySpan<byte> mySecretKeys128, 
        in PointExt4 peerPublicPoints, 
        Span<byte> sharedSecrets128)
    {
        Span<byte> clampedScalars = stackalloc byte[128];
        
        for(int i = 0; i < 4; i++)
        {
            ReadOnlySpan<byte> sk = mySecretKeys128.Slice(i * 32, 32);
            Span<byte> hash = stackalloc byte[64];
            SHA512.HashData(sk, hash);
            
            Span<byte> scalar = clampedScalars.Slice(i * 32, 32);
            hash.Slice(0, 32).CopyTo(scalar);

            scalar[0] &= 248; 
            scalar[31] &= 127; 
            scalar[31] |= 64;
        }

        // SharedSecret = PeerPublicKey * MyPrivateScalar
        Ed25519Batch.Multiply4(clampedScalars, in peerPublicPoints, out var sharedPoints);
        sharedPoints.EncodeToBytes(sharedSecrets128);
    }
}