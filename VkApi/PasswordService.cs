using System.Security.Cryptography;
using System.Text;

namespace VkApi;

// Adapted from https://code-maze.com/csharp-hashing-salting-passwords-best-practices/
public class PasswordService : IPasswordService
{
    private const int KeySize = 64;
    private const int Iterations = 100_000;
    private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;
    
    public string Encrypt(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(KeySize);
        var hash = HashWithSalt(password, salt);
        return $"{Convert.ToBase64String(salt)};{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encrypted)
    {
        var tokens = encrypted.Split(';');
        var salt = Convert.FromBase64String(tokens[0]);
        var hash = Convert.FromBase64String(tokens[1]);
        var actualHash = HashWithSalt(password, salt);
        return hash.SequenceEqual(actualHash);
    }

    private byte[] HashWithSalt(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            _hashAlgorithm,
            KeySize);
    }
}