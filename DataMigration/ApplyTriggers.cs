using Npgsql;

internal static class ApplyTriggers
{
    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true";

    public static async Task RunAsync()
    {
        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();
        Console.WriteLine("[OK] Connected to PostgreSQL");

        const string sql = """
            CREATE OR REPLACE FUNCTION update_rowversion() RETURNS trigger AS $$
            BEGIN
                NEW."RowVersion" := substring(
                    sha256(CAST(clock_timestamp() AS text)::bytea || CAST(random() AS text)::bytea)
                    FROM 1 FOR 8
                );
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DO $do$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cashiersessions_rowversion_trg') THEN
                    CREATE TRIGGER cashiersessions_rowversion_trg
                    BEFORE INSERT OR UPDATE ON "CashierSessions"
                    FOR EACH ROW EXECUTE FUNCTION update_rowversion();
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'refunds_rowversion_trg') THEN
                    CREATE TRIGGER refunds_rowversion_trg
                    BEFORE INSERT OR UPDATE ON "Refunds"
                    FOR EACH ROW EXECUTE FUNCTION update_rowversion();
                END IF;
            END;
            $do$;
            """;

        await using var cmd = new NpgsqlCommand(sql, pg);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("[OK] RowVersion triggers created/verified for CashierSessions and Refunds");

        // Verify triggers exist
        await using var check = new NpgsqlCommand(
            "SELECT tgname, tgrelid::regclass FROM pg_trigger WHERE tgname LIKE '%rowversion%'", pg);
        await using var rdr = await check.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
            Console.WriteLine($"  trigger: {rdr.GetString(0)} on {rdr.GetString(1)}");
    }
}
