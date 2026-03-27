using Core.X25519;

namespace Tests;

public class X25519Rfc7748Tests
{
    // Вектора из RFC 7748 (Section 6.1)
    private static readonly byte[] AlicePrivateKey = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
    private static readonly byte[] AlicePublicKey  = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");
    
    private static readonly byte[] BobPrivateKey   = Convert.FromHexString("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb");
    private static readonly byte[] BobPublicKey    = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");

    private static readonly byte[] ExpectedSharedSecret = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");

    [Fact]
    public void GeneratePublicKey_Alice_MatchesRfc7748()
    {
        // Подготавливаем массив из 4 одинаковых ключей Алисы
        byte[] batchAliceSecrets = new byte[128];
        for (int i = 0; i < 4; i++) AlicePrivateKey.CopyTo(batchAliceSecrets, i * 32);

        byte[] batchAlicePubKeys = new byte[128];

        // Act
        X25519DiffieHellman.GeneratePublicKeysBatch(batchAliceSecrets, batchAlicePubKeys);

        // Assert
        for (int i = 0; i < 4; i++)
        {
            var actualPub = batchAlicePubKeys.AsSpan(i * 32, 32).ToArray();
            Assert.True(AlicePublicKey.SequenceEqual(actualPub), 
                $"Lane {i} failed. Expected: {Convert.ToHexString(AlicePublicKey)}");
        }
    }

    [Fact]
    public void DeriveSharedSecret_AliceAndBob_MatchesRfc7748()
    {
        // Алиса вычисляет секрет со своей стороны
        byte[] batchAliceSecrets = new byte[128];
        byte[] batchBobPubKeys   = new byte[128];
        for (int i = 0; i < 4; i++) 
        {
            AlicePrivateKey.CopyTo(batchAliceSecrets, i * 32);
            BobPublicKey.CopyTo(batchBobPubKeys, i * 32);
        }

        byte[] batchAliceDerivedSecrets = new byte[128];
        
        // Act (Alice)
        X25519DiffieHellman.DeriveSharedSecretsBatch(batchAliceSecrets, batchBobPubKeys, batchAliceDerivedSecrets);

        // Assert
        for (int i = 0; i < 4; i++)
        {
            var actualSecret = batchAliceDerivedSecrets.AsSpan(i * 32, 32).ToArray();
            Assert.True(ExpectedSharedSecret.SequenceEqual(actualSecret), 
                $"Alice Derive Lane {i} failed.");
        }

        // Боб вычисляет секрет со своей стороны
        byte[] batchBobSecrets   = new byte[128];
        byte[] batchAlicePubKeys = new byte[128];
        for (int i = 0; i < 4; i++) 
        {
            BobPrivateKey.CopyTo(batchBobSecrets, i * 32);
            AlicePublicKey.CopyTo(batchAlicePubKeys, i * 32);
        }

        byte[] batchBobDerivedSecrets = new byte[128];

        // Act (Bob)
        X25519DiffieHellman.DeriveSharedSecretsBatch(batchBobSecrets, batchAlicePubKeys, batchBobDerivedSecrets);

        // Assert
        for (int i = 0; i < 4; i++)
        {
            var actualSecret = batchBobDerivedSecrets.AsSpan(i * 32, 32).ToArray();
            Assert.True(ExpectedSharedSecret.SequenceEqual(actualSecret), 
                $"Bob Derive Lane {i} failed.");
        }
    }
}