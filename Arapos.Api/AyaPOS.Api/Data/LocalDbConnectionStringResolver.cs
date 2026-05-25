using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Ayapos.Api.Data;

internal static class LocalDbConnectionStringResolver
{
    private static readonly Regex LocalDbPattern = new(@"^\(localdb\)\\(?<instance>.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] SqlLocalDbCandidates =
    {
        @"C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe",
        @"C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe",
        @"C:\Program Files\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe",
        "sqllocaldb"
    };

    public static string Resolve(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var builder = new SqlConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource?.Trim();

        if (string.IsNullOrWhiteSpace(dataSource))
            return connectionString;

        var match = LocalDbPattern.Match(dataSource);
        if (!match.Success)
            return connectionString;

        var instanceName = match.Groups["instance"].Value.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            return connectionString;

        TryStartLocalDb(instanceName);

        var pipeName = TryGetLocalDbPipe(instanceName);
        if (string.IsNullOrWhiteSpace(pipeName))
            return connectionString;

        builder.DataSource = pipeName;
        builder.TrustServerCertificate = true;
        return builder.ConnectionString;
    }

    private static void TryStartLocalDb(string instanceName)
    {
        try
        {
            RunSqlLocalDb($"start \"{instanceName}\"");
        }
        catch
        {
            // Keep the original connection string path if startup fails.
        }
    }

    private static string? TryGetLocalDbPipe(string instanceName)
    {
        try
        {
            var output = RunSqlLocalDb($"info \"{instanceName}\"");
            var line = output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(static x => x.StartsWith("Instance pipe name:", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(line))
                return null;

            var pipeName = line.Split(':', 2, StringSplitOptions.TrimEntries).LastOrDefault();
            return string.IsNullOrWhiteSpace(pipeName) ? null : pipeName;
        }
        catch
        {
            return null;
        }
    }

    private static string RunSqlLocalDb(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveSqlLocalDbExecutable(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);

        return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
    }

    private static string ResolveSqlLocalDbExecutable()
        => SqlLocalDbCandidates.FirstOrDefault(File.Exists) ?? SqlLocalDbCandidates.Last();
}
