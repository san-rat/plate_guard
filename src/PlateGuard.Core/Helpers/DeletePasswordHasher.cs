using System.Security.Cryptography;
using System.Text;

namespace PlateGuard.Core.Helpers;

public static class DeletePasswordHasher
{
    public static string Hash(string? value)
    {
        var input = value ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string? plainTextValue, string? hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            return false;
        }

        return StringComparer.Ordinal.Equals(Hash(plainTextValue), hashedValue);
    }
}
