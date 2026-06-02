using Npgsql;

internal static class CheckTenants
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        // Check all tenants status + license
        Console.WriteLine("=== Tenant Status + License ===");
        await using var r1 = await new NpgsqlCommand(@"
            SELECT ""Name"", ""Slug"", ""Status"", ""LicenseStatus"", ""LicenseExpiresAt"",
                   CASE WHEN ""LicenseExpiresAt"" > NOW() THEN 'valid' ELSE 'EXPIRED' END AS expiry
            FROM ""Tenants"" ORDER BY ""Slug""", pg).ExecuteReaderAsync();

        while (await r1.ReadAsync())
        {
            var expired = r1.GetString(5);
            var flag = expired == "EXPIRED" ? " ⚠ EXPIRED" : "";
            Console.WriteLine($"  {r1.GetString(0),-28} [{r1.GetString(1),-20}] " +
                              $"Status={r1.GetString(2)} Lic={r1.GetString(3)} " +
                              $"Expires={r1.GetDateTime(4):yyyy-MM-dd}{flag}");
        }

        await r1.CloseAsync();

        // Check users per tenant
        Console.WriteLine();
        Console.WriteLine("=== Users per Tenant ===");
        await using var r2 = await new NpgsqlCommand(@"
            SELECT t.""Slug"", u.""Username"", u.""Role"", u.""IsActive"",
                   u.""LicenseStatus"", u.""LicenseExpiresAt"",
                   CASE WHEN u.""LicenseExpiresAt"" > NOW() THEN 'valid' ELSE 'EXPIRED' END AS expiry,
                   EXISTS(SELECT 1 FROM ""UserPins"" p WHERE p.""UserId"" = u.""Id"" AND p.""TenantId"" = t.""Id"") AS has_pin
            FROM ""Users"" u
            JOIN ""UserPins"" pin ON pin.""UserId"" = u.""Id""
            JOIN ""Tenants"" t ON t.""Id"" = pin.""TenantId""
            ORDER BY t.""Slug"", u.""Role"", u.""Username""", pg).ExecuteReaderAsync();

        string lastSlug = "";
        while (await r2.ReadAsync())
        {
            var slug = r2.GetString(0);
            if (slug != lastSlug) { Console.WriteLine($"\n  [{slug}]"); lastSlug = slug; }
            var expiry = r2.GetString(6);
            var expiryFlag = expiry == "EXPIRED" ? " ⚠LIC-EXPIRED" : "";
            Console.WriteLine($"    {r2.GetString(1),-20} Role={r2.GetString(2),-15} " +
                              $"Active={r2.GetBoolean(3)} LicStatus={r2.GetString(4)}" +
                              $"{expiryFlag} HasPin={r2.GetBoolean(7)}");
        }
    }
}
