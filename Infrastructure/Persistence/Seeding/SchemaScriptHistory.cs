using System.Data;
using Dapper;

namespace Infrastructure.Persistence.Seeding;

/// <summary>
/// Records which schema scripts have been applied, so the runner can be re-run safely.
///
/// This matters because the dated scripts are one-time data migrations, not declarative
/// DDL: 20260826_RestructureCategoriesOriginalsPrints rewrites the old taxonomy into the
/// new one, and re-applying it to an already-restructured database corrupts it. Tracking
/// is the same approach EF Core takes with __EFMigrationsHistory.
/// </summary>
internal static class SchemaScriptHistory
{
    private const string Table = "SchemaScriptHistory";

    public static async Task EnsureTableAsync(IDbConnection connection, CancellationToken ct)
    {
        await connection.ExecuteAsync(new CommandDefinition($"""
            CREATE TABLE IF NOT EXISTS {Table} (
                ScriptName TEXT        PRIMARY KEY,
                AppliedAt  TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """, cancellationToken: ct));
    }

    public static async Task<bool> IsAppliedAsync(IDbConnection connection, string scriptName, CancellationToken ct)
        => await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            $"SELECT EXISTS (SELECT 1 FROM {Table} WHERE ScriptName = @scriptName);",
            new { scriptName }, cancellationToken: ct));

    public static async Task RecordAsync(IDbConnection connection, string scriptName, CancellationToken ct)
        => await connection.ExecuteAsync(new CommandDefinition(
            $"INSERT INTO {Table} (ScriptName) VALUES (@scriptName) ON CONFLICT DO NOTHING;",
            new { scriptName }, cancellationToken: ct));
}
