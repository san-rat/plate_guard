using System.Security.Cryptography;
using System.Text;

namespace PlateGuard.Core.Helpers;

public static class DeletePasswordHasher
{
    private const string Pbkdf2Prefix = "PBKDF2";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string? value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            value ?? string.Empty,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Pbkdf2Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string? plainTextValue, string? hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            return false;
        }

        if (hashedValue.StartsWith($"{Pbkdf2Prefix}$", StringComparison.Ordinal))
        {
            return VerifyPbkdf2(plainTextValue ?? string.Empty, hashedValue);
        }

        return VerifyLegacySha256(plainTextValue ?? string.Empty, hashedValue);
    }

    public static bool NeedsUpgrade(string? stored)
    {
        return !string.IsNullOrWhiteSpace(stored) &&
               !stored.StartsWith($"{Pbkdf2Prefix}$", StringComparison.Ordinal);
    }

    private static bool VerifyPbkdf2(string plainTextValue, string hashedValue)
    {
        try
        {
            var parts = hashedValue.Split('$');
            if (parts.Length != 4 ||
                !string.Equals(parts[0], Pbkdf2Prefix, StringComparison.Ordinal) ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations <= 0)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                plainTextValue,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            return false;
        }
    }

    private static bool VerifyLegacySha256(string plainTextValue, string hashedValue)
    {
        if (hashedValue.Length != 64 || !IsHex(hashedValue))
        {
            return false;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextValue));
        var expectedHash = Convert.FromHexString(hashedValue);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }

    private static bool IsHex(string value)
    {
        return value.All(character =>
            character is >= '0' and <= '9' ||
            character is >= 'a' and <= 'f' ||
            character is >= 'A' and <= 'F');
    }
}
