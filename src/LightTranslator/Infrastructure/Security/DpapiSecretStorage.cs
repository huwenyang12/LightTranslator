using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LightTranslator.Infrastructure.Security;

public sealed class DpapiSecretStorage : ISecretStorage
{
    private readonly string _baseDirectory;

    public DpapiSecretStorage(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ??
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "Bridgo",
                "secrets"
            );
    }

    public void Save(string name, string secret)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Secret name cannot be empty.",
                nameof(name)
            );
        }

        Directory.CreateDirectory(_baseDirectory);

        var plaintextBytes = Encoding.UTF8.GetBytes(secret);

        var encryptedBytes = ProtectedData.Protect(
            plaintextBytes,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser
        );

        File.WriteAllBytes(
            GetFilePath(name),
            encryptedBytes
        );
    }

    public string? Load(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Secret name cannot be empty.",
                nameof(name)
            );
        }

        var filePath = GetFilePath(name);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var encryptedBytes = File.ReadAllBytes(filePath);

        var plaintextBytes = ProtectedData.Unprotect(
            encryptedBytes,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser
        );

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public void Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Secret name cannot be empty.",
                nameof(name)
            );
        }

        var filePath = GetFilePath(name);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetFilePath(string name)
    {
        return Path.Combine(
            _baseDirectory,
            $"{name}.bin"
        );
    }
}