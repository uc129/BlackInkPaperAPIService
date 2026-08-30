using System.Data;
using Dapper;

namespace Infrastructure.Persistence.Seeding;

/// <summary>
/// Locates and executes a schema or seed script. The scripts manage their own transactions
/// (BEGIN; ... COMMIT;), so nothing here opens an ambient one.
/// </summary>
internal static class SqlSeedScripts
{
    public static async Task ExecuteAsync(IDapperContext dapperContext, string folder, string scriptName, CancellationToken ct)
    {
        var path = Locate(folder, scriptName)
            ?? throw new FileNotFoundException(
                $"Script '{scriptName}' not found. Expected it beside the binaries in " +
                $"Persistence/{folder}, or in the Infrastructure project.");

        var sql = await File.ReadAllTextAsync(path, ct);

        using var connection = dapperContext.CreateConnection();
        if (connection.State != ConnectionState.Open) connection.Open();
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    private static string? Locate(string folder, string scriptName)
    {
        // Copied next to the binaries by the Infrastructure .csproj.
        var published = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Persistence", folder, scriptName);
        if (File.Exists(published)) return published;

        // Running from source: walk up to the solution root and look in the Infrastructure project.
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !dir.GetFiles("*.sln").Any()) dir = dir.Parent;
        if (dir is null) return null;

        var fromSource = Path.Combine(dir.FullName, "Infrastructure", "Persistence", folder, scriptName);
        return File.Exists(fromSource) ? fromSource : null;
    }
}
