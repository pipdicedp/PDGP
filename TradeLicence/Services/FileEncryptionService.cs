using System.Security.Cryptography;

namespace TradeLicence.Services
{
    public interface IFileEncryptionService
    {
        /// <summary>Encrypts raw file bytes. Returns the ciphertext and the IV
        /// that must be stored alongside it — decryption is impossible without it.</summary>
        (byte[] CipherBytes, byte[] IV) Encrypt(byte[] plainBytes);

        /// <summary>Decrypts ciphertext previously produced by Encrypt(), using its IV.</summary>
        byte[] Decrypt(byte[] cipherBytes, byte[] iv);
    }

    public class FileEncryptionService : IFileEncryptionService
    {
        private readonly byte[] _key;

        public FileEncryptionService(IConfiguration configuration)
        {
            var keyBase64 = configuration["FileEncryption:Key"];
            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                throw new InvalidOperationException(
                    "FileEncryption:Key is missing from configuration. Add a base64-encoded 32-byte AES key under \"FileEncryption\": { \"Key\": \"...\" } in appsettings.json.");
            }

            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
            {
                throw new InvalidOperationException(
                    $"FileEncryption:Key must decode to exactly 32 bytes for AES-256 (got {_key.Length}).");
            }
        }

        public (byte[] CipherBytes, byte[] IV) Encrypt(byte[] plainBytes)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // fresh, random IV per file — required, never reuse an IV with the same key

            using var encryptor = aes.CreateEncryptor();
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return (cipherBytes, aes.IV);
        }

        public byte[] Decrypt(byte[] cipherBytes, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        }
    }
}
