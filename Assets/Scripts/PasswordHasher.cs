using System;
using System.Security.Cryptography;
using System.Text;

// Passwords are never stored as plain text: each account keeps a random salt and a SHA-256 hash of salt + password.
public static class PasswordHasher
{
    const int SaltByteCount = 16;

    public static string CreateSalt()
    {
        byte[] saltBytes = new byte[SaltByteCount];
        using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            randomNumberGenerator.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    public static string Hash(string password, string salt)
    {
        using (SHA256 sha256 = SHA256.Create())
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
    }
}
