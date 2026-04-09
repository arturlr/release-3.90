using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Security;

namespace Nop.Services.Security;

/// <summary>
/// Encryption service interface
/// </summary>
public interface IEncryptionService
{
    string CreateSaltKey(int size);
    string CreatePasswordHash(string password, string saltKey, string passwordFormat = "SHA1");
    string CreateHash(byte[] data, string hashAlgorithm = "SHA1");
    string EncryptText(string plainText, string encryptionPrivateKey = "");
    string DecryptText(string cipherText, string encryptionPrivateKey = "");
}

/// <summary>
/// Encryption service — modernized from TripleDES to AES, keeps legacy hash support for migration.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly SecuritySettings _securitySettings;

    public EncryptionService(SecuritySettings securitySettings)
    {
        _securitySettings = securitySettings;
    }

    public virtual string CreateSaltKey(int size)
    {
        var buff = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(buff);
    }

    public virtual string CreatePasswordHash(string password, string saltKey, string passwordFormat = "SHA1")
    {
        return CreateHash(Encoding.UTF8.GetBytes(string.Concat(password, saltKey)), passwordFormat);
    }

    public virtual string CreateHash(byte[] data, string hashAlgorithm = "SHA1")
    {
        if (string.IsNullOrEmpty(hashAlgorithm))
            hashAlgorithm = "SHA1";

        var hashBytes = hashAlgorithm.ToUpperInvariant() switch
        {
            "SHA1" => SHA1.HashData(data),
            "SHA256" => SHA256.HashData(data),
            "SHA384" => SHA384.HashData(data),
            "SHA512" => SHA512.HashData(data),
            "MD5" => MD5.HashData(data),
            _ => throw new ArgumentException($"Unrecognized hash algorithm: {hashAlgorithm}")
        };

        return Convert.ToHexString(hashBytes);
    }

    public virtual string EncryptText(string plainText, string encryptionPrivateKey = "")
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (string.IsNullOrEmpty(encryptionPrivateKey))
            encryptionPrivateKey = _securitySettings.EncryptionKey ?? string.Empty;

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(encryptionPrivateKey[..16]);
        aes.IV = Encoding.UTF8.GetBytes(encryptionPrivateKey[8..24]);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = aes.EncryptCbc(plainBytes, aes.IV);
        return Convert.ToBase64String(encrypted);
    }

    public virtual string DecryptText(string cipherText, string encryptionPrivateKey = "")
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (string.IsNullOrEmpty(encryptionPrivateKey))
            encryptionPrivateKey = _securitySettings.EncryptionKey ?? string.Empty;

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(encryptionPrivateKey[..16]);
        aes.IV = Encoding.UTF8.GetBytes(encryptionPrivateKey[8..24]);

        var cipherBytes = Convert.FromBase64String(cipherText);
        var decrypted = aes.DecryptCbc(cipherBytes, aes.IV);
        return Encoding.UTF8.GetString(decrypted);
    }
}
