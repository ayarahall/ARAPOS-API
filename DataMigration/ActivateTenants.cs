using Npgsql;

internal static class ActivateTenants
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"UPDATE ""Tenants"" SET ""Status"" = 'ACTIVE' WHERE ""Status"" != 'ACTIVE'", pg);
        int rows = await cmd.ExecuteNonQueryAsync();

        await using var sel = new NpgsqlCommand(
            @"SELECT ""Name"", ""Slug"", ""Status"" FROM ""Tenants"" ORDER BY ""Name""", pg);
        await using var r = await sel.ExecuteReaderAsync();

        Console.WriteLine($"Activated {rows} tenant(s). All tenants now:");
        while (await r.ReadAsync())
            Console.WriteLine($"  {r.GetString(0),-30} [{r.GetString(1)}] → {r.GetString(2)}");
    }
}
