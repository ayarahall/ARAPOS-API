using Microsoft.Data.SqlClient;
using Npgsql;

internal static class Patch
{
    private const string SQL_CONN =
        "Server=DESKTOP-QKHC9EC\\AYAPOS;Database=AYAPOS;" +
        "Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

    private const string PG_CONN =
        "Host=dpg-d8e712sm0tmc73ejprdg-a.oregon-postgres.render.com;Port=5432;" +
        "Database=ayapos;Username=ayapos;Password=2GZVASubompHYiHqZ8a2wpgzV1RChNKo;" +
        "SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=120";

    private static readonly Guid BranchId = Guid.Parse("4d92e07e-a52f-4fd4-9403-bdc28239402a");
    private static readonly Guid TenantId = Guid.Parse("918799ec-cdd2-4f0a-983e-d1691a9910ff");
    private static readonly DateTime CreatedAt = new(2026, 2, 13, 20, 16, 37, DateTimeKind.Utc);

    private static readonly (Guid Id, string Name, string Phone, string Job)[] StaffToInsert =
    [
        (Guid.Parse("10d54ea0-dd84-44ec-bbab-364ab48b2094"), "نور",  "0500000002", "Nails"),
        (Guid.Parse("5a01957e-8341-4a7e-9220-f40e526d4a59"), "سارة", "0500000001", "Hair"),
    ];

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Patch: inserting 2 staff + their appointments ===");

        using var pg = new NpgsqlConnection(PG_CONN);
        await pg.OpenAsync();

        // Insert the 2 staff assigned to first branch
        foreach (var s in StaffToInsert)
        {
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO ""Staff"" (
                    ""Id"", ""TenantId"", ""BranchId"", ""FullName"", ""Phone"", ""JobTitle"",
                    ""IsActive"", ""EmploymentType"", ""SalaryType"", ""TrackAttendance"",
                    ""IsBookableForAppointments"", ""CreatedAt""
                ) VALUES (
                    @id, @tenantId, @branchId, @name, @phone, @job,
                    true, 'employee', 'monthly', true, false, @createdAt
                ) ON CONFLICT DO NOTHING", pg);

            cmd.Parameters.AddWithValue("@id", s.Id);
            cmd.Parameters.AddWithValue("@tenantId", TenantId);
            cmd.Parameters.AddWithValue("@branchId", BranchId);
            cmd.Parameters.AddWithValue("@name", s.Name);
            cmd.Parameters.AddWithValue("@phone", s.Phone);
            cmd.Parameters.AddWithValue("@job", s.Job);
            cmd.Parameters.AddWithValue("@createdAt", CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"  Inserted staff: {s.Name}");
        }

        // Retry appointments that referenced those staff
        var staffIds = string.Join(",", StaffToInsert.Select(s => $"'{s.Id}'"));
        using var sql = new SqlConnection(SQL_CONN);
        await sql.OpenAsync();

        await using var apptCmd = new SqlCommand(
            "SELECT Id, TenantId, BranchId, CustomerId, StaffId, StartAt, EndAt, Status, Notes, CreatedAt, CreatedByUserId " +
            $"FROM Appointments WHERE StaffId IN ({staffIds})", sql);

        await using var ar = await apptCmd.ExecuteReaderAsync();
        int apptCount = 0;
        while (await ar.ReadAsync())
        {
            var id     = ar.GetGuid(0);
            var tid    = ar.GetGuid(1);
            var bid    = ar.IsDBNull(2) ? (Guid?)null : ar.GetGuid(2);
            var cid    = ar.IsDBNull(3) ? (Guid?)null : ar.GetGuid(3);
            var sid    = ar.IsDBNull(4) ? (Guid?)null : ar.GetGuid(4);
            var start  = DateTime.SpecifyKind(ar.GetDateTime(5), DateTimeKind.Utc);
            var end    = DateTime.SpecifyKind(ar.GetDateTime(6), DateTimeKind.Utc);
            var status = ar.GetString(7);
            var notes  = ar.IsDBNull(8) ? null : ar.GetString(8);
            var cat    = DateTime.SpecifyKind(ar.GetDateTime(9), DateTimeKind.Utc);
            var cby    = ar.IsDBNull(10) ? (Guid?)null : ar.GetGuid(10);

            await using var ins = new NpgsqlCommand(@"
                INSERT INTO ""Appointments"" (
                    ""Id"", ""TenantId"", ""BranchId"", ""CustomerId"", ""StaffId"",
                    ""StartAt"", ""EndAt"", ""Status"", ""Notes"", ""CreatedAt"", ""CreatedByUserId""
                ) VALUES (
                    @id, @tid, @bid, @cid, @sid,
                    @start, @end, @status, @notes, @cat, @cby
                ) ON CONFLICT DO NOTHING", pg);

            ins.Parameters.AddWithValue("@id", id);
            ins.Parameters.AddWithValue("@tid", tid);
            ins.Parameters.AddWithValue("@bid", bid.HasValue ? (object)bid.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@cid", cid.HasValue ? (object)cid.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@sid", sid.HasValue ? (object)sid.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@start", start);
            ins.Parameters.AddWithValue("@end", end);
            ins.Parameters.AddWithValue("@status", status);
            ins.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            ins.Parameters.AddWithValue("@cat", cat);
            ins.Parameters.AddWithValue("@cby", cby.HasValue ? (object)cby.Value : DBNull.Value);

            await ins.ExecuteNonQueryAsync();
            apptCount++;
            Console.WriteLine($"  Inserted appointment {id}");
        }

        Console.WriteLine($"=== Patch done: 2 staff + {apptCount} appointment(s) inserted ===");
    }
}
