using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

if (args.Contains("--patch"))           { await Patch.RunAsync();           return; }
if (args.Contains("--verify"))          { await Verify.RunAsync();          return; }
if (args.Contains("--activate-tenants")){ await ActivateTenants.RunAsync(); return; }
if (args.Contains("--check-tenants"))   { await CheckTenants.RunAsync();   return; }
if (args.Contains("--fix-licenses"))    { await FixLicenses.RunAsync();    return; }

const string SQL_CONN =
    "Server=DESKTOP-QKHC9EC\\AYAPOS;Database=AYAPOS;" +
    "Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30";

const string PG_CONN =
    "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
    "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
    "SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=300;" +
    "Include Error Detail=true";

// Max rows per multi-value INSERT: PostgreSQL limit is 65535 params total.
// We compute per-table below; 500 is a safe default for most tables.
const int BATCH_SIZE = 500;

var insertOrder = new[]
{
    "AppSettings",
    "Tenants",
    "Licenses",
    "Users",
    "Customers",
    "LicenseActivations",
    "Branches",
    "BranchSettings",
    "BranchUserAssignments",
    "UserPins",
    "UserPinsHistory",
    "Products",
    "ProductPrices",
    "ProductStockSnapshot",
    "Services",
    "ServicePrices",
    "Invoices",
    "InvoiceSequences",
    "InvoiceItems",
    "Payments",
    "CustomerLicenses",
    "BranchExpenses",
    "InventoryMoves",
    "Staff",
    "StaffShifts",
    "StaffAttendances",
    "StaffDocuments",
    "StaffLeaves",
    "ManagerApprovals",
    "CashierSessions",
    "Refunds",
    "Appointments",
    "AppointmentItems",
};

var truncateOrder = insertOrder.Reverse().ToArray();

Console.WriteLine("=== AyaPOS Data Migration: SQL Server → PostgreSQL ===");
Console.WriteLine();

using var sqlConn = new SqlConnection(SQL_CONN);
await sqlConn.OpenAsync();
Console.WriteLine("[OK] Connected to SQL Server");

using var pgConn = new NpgsqlConnection(PG_CONN);
await pgConn.OpenAsync();
Console.WriteLine("[OK] Connected to PostgreSQL (Render)");
Console.WriteLine();

// Step 1: Truncate all PG tables (reverse FK order)
Console.WriteLine("--- Step 1: Clearing PostgreSQL tables ---");
foreach (var table in truncateOrder)
{
    await using var checkCmd = new NpgsqlCommand(
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name=@t",
        pgConn);
    checkCmd.Parameters.AddWithValue("@t", table);
    var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync()!) > 0;
    if (!exists) { Console.WriteLine($"  {table}: not in PG, skip"); continue; }

    await using var truncCmd = new NpgsqlCommand($"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE", pgConn);
    await truncCmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  {table}: truncated");
}
Console.WriteLine();

async Task<Dictionary<string, string>> GetSqlServerColTypes(SqlConnection conn, string table)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    await using var cmd = new SqlCommand(
        "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
        "WHERE TABLE_NAME=@t AND TABLE_SCHEMA='dbo' ORDER BY ORDINAL_POSITION", conn);
    cmd.Parameters.AddWithValue("@t", table);
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
        dict[rdr.GetString(0)] = rdr.GetString(1);
    return dict;
}

async Task<List<string>> GetPgColumns(NpgsqlConnection conn, string table)
{
    var list = new List<string>();
    await using var cmd = new NpgsqlCommand(
        "SELECT column_name FROM information_schema.columns " +
        "WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn);
    cmd.Parameters.AddWithValue("@t", table);
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
        list.Add(rdr.GetString(0));
    return list;
}

static object ConvertForPg(object val, string sqlType)
{
    if (val is DBNull) return DBNull.Value;

    return sqlType.ToLowerInvariant() switch
    {
        "bit" => Convert.ToBoolean(val),
        "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" =>
            val is DateTimeOffset dto
                ? dto.UtcDateTime
                : val is DateTime dt
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : val,
        "uniqueidentifier" => val is Guid g ? g : Guid.Parse(val.ToString()!),
        "timestamp" or "rowversion" => val,
        _ => val
    };
}

// Step 2: Bulk insert table by table using multi-row VALUES batches
Console.WriteLine("--- Step 2: Inserting data ---");
int totalRows = 0;

foreach (var table in insertOrder)
{
    await using var sqlCheck = new SqlCommand(
        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@t AND TABLE_SCHEMA='dbo' AND TABLE_TYPE='BASE TABLE'",
        sqlConn);
    sqlCheck.Parameters.AddWithValue("@t", table);
    var sqlExists = Convert.ToInt32(await sqlCheck.ExecuteScalarAsync()!) > 0;
    if (!sqlExists) { Console.WriteLine($"  {table}: not in SQL Server, skip"); continue; }

    await using var pgCheckCmd = new NpgsqlCommand(
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name=@t",
        pgConn);
    pgCheckCmd.Parameters.AddWithValue("@t", table);
    var pgExists = Convert.ToInt64(await pgCheckCmd.ExecuteScalarAsync()!) > 0;
    if (!pgExists) { Console.WriteLine($"  {table}: not in PostgreSQL, skip"); continue; }

    var sqlColTypes = await GetSqlServerColTypes(sqlConn, table);
    var pgCols = await GetPgColumns(pgConn, table);
    var commonCols = sqlColTypes.Keys
        .Where(c => pgCols.Any(p => string.Equals(p, c, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    if (commonCols.Count == 0) { Console.WriteLine($"  {table}: no common columns, skip"); continue; }

    // Max safe batch size given PostgreSQL's 65535-parameter limit
    int batchSize = Math.Min(BATCH_SIZE, Math.Max(1, 65000 / commonCols.Count));
    string colList = string.Join(", ", commonCols.Select(c => $"\"{c}\""));

    // Load all rows from SQL Server (local, fast)
    var rows = new List<object[]>();
    await using (var sel = new SqlCommand($"SELECT {string.Join(", ", commonCols.Select(c => $"[{c}]"))} FROM [{table}] WITH (NOLOCK)", sqlConn))
    {
        sel.CommandTimeout = 300;
        await using var rdr = await sel.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            var row = new object[commonCols.Count];
            for (int i = 0; i < commonCols.Count; i++)
            {
                var sqlType = sqlColTypes.TryGetValue(commonCols[i], out var t) ? t : "nvarchar";
                object raw = rdr.IsDBNull(i) ? DBNull.Value : rdr.GetValue(i);
                row[i] = ConvertForPg(raw, sqlType);
            }
            rows.Add(row);
        }
    }

    if (rows.Count == 0) { Console.WriteLine($"  {table}: 0 rows (empty)"); continue; }

    // Insert in multi-row batches — one round-trip per batchSize rows
    int count = 0;
    int errors = 0;
    for (int offset = 0; offset < rows.Count; offset += batchSize)
    {
        var batch = rows.Skip(offset).Take(batchSize).ToList();

        // Build: INSERT INTO "T" (cols) VALUES (@r0c0,@r0c1,...),(@r1c0,...) ON CONFLICT DO NOTHING
        var valuePlaceholders = batch.Select((_, ri) =>
            $"({string.Join(", ", Enumerable.Range(0, commonCols.Count).Select(ci => $"@r{ri}c{ci}"))})");
        string sql = $"INSERT INTO \"{table}\" ({colList}) VALUES {string.Join(", ", valuePlaceholders)} ON CONFLICT DO NOTHING";

        await using var cmd = new NpgsqlCommand(sql, pgConn);
        for (int ri = 0; ri < batch.Count; ri++)
            for (int ci = 0; ci < commonCols.Count; ci++)
                cmd.Parameters.AddWithValue($"@r{ri}c{ci}", batch[ri][ci]);

        try
        {
            await cmd.ExecuteNonQueryAsync();
            count += batch.Count;
        }
        catch (Exception ex)
        {
            errors++;
            Console.WriteLine($"\n  [{table}] batch {offset / batchSize + 1} failed: {ex.Message}");
            // Fall back: insert this batch row-by-row
            string rowSql = $"INSERT INTO \"{table}\" ({colList}) VALUES ({string.Join(", ", Enumerable.Range(0, commonCols.Count).Select(ci => $"@p{ci}"))}) ON CONFLICT DO NOTHING";
            foreach (var row in batch)
            {
                await using var rowCmd = new NpgsqlCommand(rowSql, pgConn);
                for (int ci = 0; ci < commonCols.Count; ci++)
                    rowCmd.Parameters.AddWithValue($"@p{ci}", row[ci]);
                try { await rowCmd.ExecuteNonQueryAsync(); count++; }
                catch { /* skip bad row */ }
            }
        }
    }

    string errNote = errors > 0 ? $" ({errors} batch errors, used row-by-row fallback)" : "";
    Console.WriteLine($"  {table}: {count} rows inserted{errNote}");
    totalRows += count;
}

Console.WriteLine();
Console.WriteLine($"=== Migration complete. {totalRows} total rows imported. ===");
