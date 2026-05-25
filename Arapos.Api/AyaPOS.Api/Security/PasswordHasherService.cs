using System.Security.Cryptography;
using System.Text;

namespace Ayapos.Api.Security;

public sealed class PasswordHasherService
{
    private const string Scheme = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            '$',
            Scheme,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string? storedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(storedPassword))
            return PasswordVerificationResult.Failed;

        if (storedPassword == "PIN_ONLY")
            return PasswordVerificationResult.Failed;

        if (TryVerifyHashed(storedPassword, providedPassword, out var valid))
        {
            return valid
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }

        var storedBytes = Encoding.UTF8.GetBytes(storedPassword);
        var providedBytes = Encoding.UTF8.GetBytes(providedPassword ?? string.Empty);
        var matches = CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes);

        return matches
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    private static bool TryVerifyHashed(string storedPassword, string providedPassword, out bool isValid)
    {
        isValid = false;

        var parts = storedPassword.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], Scheme, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(parts[1], out var iterations) || iterations < 10_000)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(providedPassword ?? string.Empty),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            isValid = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public enum PasswordVerificationResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
