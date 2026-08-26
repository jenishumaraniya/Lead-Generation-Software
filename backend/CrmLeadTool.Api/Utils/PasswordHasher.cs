using System.Security.Cryptography;

namespace CrmLeadTool.Api.Utils;

public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 10000;

    public static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        string salt = Convert.ToBase64String(saltBytes);

        using var algorithm = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(algorithm.GetBytes(KeySize));

        return (hash, salt);
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        using var algorithm = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        string newHash = Convert.ToBase64String(algorithm.GetBytes(KeySize));

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hash),
            Convert.FromBase64String(newHash)
        );
    }
}
