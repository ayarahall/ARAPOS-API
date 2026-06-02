using Npgsql;

internal static class Verify
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=120";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        var tables = new[]
        {
            "Tenants", "Users", "Branches", "BranchSettings", "BranchUserAssignments",
            "Services", "ServicePrices", "Products", "ProductPrices",
            "Customers", "Invoices", "InvoiceItems", "Payments",
            "CustomerLicenses", "BranchExpenses", "Staff",
            "Appointments", "AppointmentItems", "CashierSessions",
            "UserPins", "InventoryMoves"
        };

        Console.WriteLine("=== PostgreSQL Row Counts (Render) ===");
        int grand = 0;
        foreach (var t in tables)
        {
            await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{t}\"", pg);
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync()!);
            Console.WriteLine($"  {t,-30} {count,5}");
            grand += (int)count;
        }
        Console.WriteLine($"  {"TOTAL",-30} {grand,5}");

        Console.WriteLine();
        Console.WriteLine("=== Sample Tenant Data ===");
        await using var tCmd = new NpgsqlCommand(
            "SELECT \"Name\", \"Slug\", \"Status\", \"LicenseStatus\", \"LicenseExpiresAt\" FROM \"Tenants\" ORDER BY \"Name\"",
            pg);
        await using var tr = await tCmd.ExecuteReaderAsync();
        while (await tr.ReadAsync())
            Console.WriteLine($"  {tr.GetString(0),-30} slug={tr.GetString(1),-20} {tr.GetString(2)} / {tr.GetString(3)}");

        Console.WriteLine();
        Console.WriteLine("=== Sample User Data ===");
        await using var uCmd = new NpgsqlCommand(
            "SELECT \"Username\", \"Role\", \"IsActive\", \"LicenseStatus\" FROM \"Users\" ORDER BY \"Role\", \"Username\"",
            pg);
        await using var ur = await uCmd.ExecuteReaderAsync();
        while (await ur.ReadAsync())
            Console.WriteLine($"  {ur.GetString(0),-20} role={ur.GetString(1),-15} active={ur.GetBoolean(2)} lic={ur.GetString(3)}");
    }
}
