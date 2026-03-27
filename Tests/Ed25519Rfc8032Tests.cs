using Core.Ed25519;

namespace Tests;

public class Ed25519Rfc8032Tests
{
    [Fact]
    public void GeneratePublicKey_MatchesRfc8032_TestVector1()
    {
        // RFC 8032 Test Vector 1
        // ALGORITHM: Ed25519
        // SECRET KEY: 9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60
        // PUBLIC KEY: d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a
        
        byte[] secretKey = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
        byte[] expectedPubKey = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
        
        // Подготавливаем батч из 4 одинаковых ключей
        byte[] batchSecretKeys = new byte[128];
        for (int i = 0; i < 4; i++)
        {
            Array.Copy(secretKey, 0, batchSecretKeys, i * 32, 32);
        }
        
        byte[] batchPublicKeys = new byte[128];
        
        // Act
        Ed25519DiffieHellman.GeneratePublicKeysBatch(batchSecretKeys, batchPublicKeys);
        
        // Assert
        for (int i = 0; i < 4; i++)
        {
            byte[] actualPubKey = batchPublicKeys.AsSpan(i * 32, 32).ToArray();
            Assert.True(expectedPubKey.SequenceEqual(actualPubKey),
                $"Lane {i} failed. \nExpected: {Convert.ToHexString(expectedPubKey)}\nActual:   {Convert.ToHexString(actualPubKey)}");
        }
    }
}