using Npgsql;

internal static class TestInsert
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        // 1. Check triggers exist
        Console.WriteLine("=== Triggers ===");
        await using var t1 = new NpgsqlCommand(
            "SELECT tgname, tgrelid::text FROM pg_trigger WHERE tgname LIKE '%rowversion%'", pg);
        await using var r1 = await t1.ExecuteReaderAsync();
        bool found = false;
        while (await r1.ReadAsync()) { Console.WriteLine($"  {r1.GetString(0)} on {r1.GetString(1)}"); found = true; }
        if (!found) Console.WriteLine("  NO triggers found!");
        await r1.CloseAsync();

        // 2. Try a raw INSERT with RETURNING to simulate what EF Core does
        Console.WriteLine("\n=== Raw INSERT test ===");
        var id = Guid.NewGuid();
        var tenantId = Guid.Parse("290386a9-0fd1-4712-9d7c-657900128365");
        var branchId = Guid.Parse("29c6e565-5c1a-41a3-bc6b-2a7a9bd2d701");
        var userId   = Guid.NewGuid();

        try
        {
            await using var ins = new NpgsqlCommand("""
                INSERT INTO "CashierSessions"
                    ("Id","TenantId","BranchId","UserId","OpenedAt","OpeningCashCents",
                     "TotalCashCents","TotalCardCents","TotalTransferCents",
                     "TotalRefundCents","ExpectedCashCents","ActualCashCents",
                     "DifferenceCents","IsClosed")
                VALUES (@id,@tid,@bid,@uid,NOW(),0, 0,0,0, 0,0,0, 0,false)
                RETURNING "RowVersion"
                """, pg);
            ins.Parameters.AddWithValue("@id",  id);
            ins.Parameters.AddWithValue("@tid", tenantId);
            ins.Parameters.AddWithValue("@bid", branchId);
            ins.Parameters.AddWithValue("@uid", userId);

            var rv = await ins.ExecuteScalarAsync();
            Console.WriteLine($"  INSERT OK — RowVersion = {(rv == null ? "null" : Convert.ToHexString((byte[])rv))}");

            // Clean up
            await using var del = new NpgsqlCommand($"DELETE FROM \"CashierSessions\" WHERE \"Id\" = @id", pg);
            del.Parameters.AddWithValue("@id", id);
            await del.ExecuteNonQueryAsync();
            Console.WriteLine("  Test row deleted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  INSERT FAILED: {ex.Message}");
        }
    }
}
