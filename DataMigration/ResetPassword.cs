using System.Security.Cryptography;
using System.Text;
using Npgsql;

internal static class ResetPassword
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync(string username, string newPassword)
    {
        var hash = HashPassword(newPassword);

        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"UPDATE ""Users"" SET ""PasswordHash"" = @hash WHERE ""Username"" = @user", pg);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@user", username);
        var rows = await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"Updated {rows} row(s) for user '{username}'.");
    }

    private static string HashPassword(string password)
    {
        const int saltSize = 16, keySize = 32, iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations,
            HashAlgorithmName.SHA256, keySize);
        return $"PBKDF2-SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
