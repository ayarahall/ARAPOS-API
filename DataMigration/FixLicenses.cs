using Npgsql;

internal static class FixLicenses
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        // 1. Extend all expired tenant licenses to 2028-12-31
        await using var t1 = new NpgsqlCommand(@"
            UPDATE ""Tenants""
            SET ""LicenseExpiresAt"" = '2028-12-31T00:00:00Z'
            WHERE ""LicenseExpiresAt"" < NOW()", pg);
        int tenantRows = await t1.ExecuteNonQueryAsync();
        Console.WriteLine($"Extended {tenantRows} expired tenant license(s) to 2028-12-31.");

        // 2. Extend all expired user licenses to 2028-12-31
        await using var t2 = new NpgsqlCommand(@"
            UPDATE ""Users""
            SET ""LicenseExpiresAt"" = '2028-12-31T00:00:00Z'
            WHERE ""LicenseExpiresAt"" < NOW()", pg);
        int userRows = await t2.ExecuteNonQueryAsync();
        Console.WriteLine($"Extended {userRows} expired user license(s) to 2028-12-31.");

        // 3. Reset aya.r password to match owner (Admin1234)
        //    Copy the hash directly from the owner user
        await using var hashCmd = new NpgsqlCommand(@"
            SELECT ""PasswordHash"" FROM ""Users"" WHERE ""Username"" = 'owner' LIMIT 1", pg);
        var ownerHash = (string?)await hashCmd.ExecuteScalarAsync();

        if (ownerHash != null)
        {
            await using var resetCmd = new NpgsqlCommand(@"
                UPDATE ""Users"" SET ""PasswordHash"" = @hash
                WHERE ""Username"" = 'aya.r'", pg);
            resetCmd.Parameters.AddWithValue("@hash", ownerHash);
            int resetRows = await resetCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Reset aya.r password to Admin1234 ({resetRows} row updated).");
        }

        // 4. Show final tenant status
        Console.WriteLine();
        Console.WriteLine("=== All tenants after fix ===");
        await using var check = new NpgsqlCommand(@"
            SELECT ""Name"", ""Slug"", ""LicenseExpiresAt"",
                   CASE WHEN ""LicenseExpiresAt"" > NOW() THEN 'OK' ELSE 'EXPIRED' END
            FROM ""Tenants"" ORDER BY ""Slug""", pg);
        await using var r = await check.ExecuteReaderAsync();
        while (await r.ReadAsync())
            Console.WriteLine($"  {r.GetString(0),-28} [{r.GetString(1),-20}] expires={r.GetDateTime(2):yyyy-MM-dd} {r.GetString(3)}");
    }
}
