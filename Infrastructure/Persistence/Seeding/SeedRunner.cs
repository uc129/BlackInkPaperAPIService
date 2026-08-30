using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeding;

public sealed class SeedRunner(
    IServiceProvider services,
    IDapperContext dapperContext,
    ILogger<SeedRunner> logger) : ISeedRunner
{
    public IReadOnlyList<string> AvailableSets => SeedSets.Definitions.Keys.ToArray();

    public async Task<SeedReport> RunAsync(string setName, CancellationToken ct = default)
    {
        if (!SeedSets.Definitions.TryGetValue(setName, out var steps))
        {
            return new SeedReport(setName, [], false,
                $"Unknown seed set '{setName}'. Available: {string.Join(", ", AvailableSets)}.");
        }

        var completed = new List<string>();
        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();
            logger.LogInformation("Seeding step: {Step}", step.Name);

            try
            {
                await RunStepAsync(step, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seed step failed: {Step}", step.Name);
                return new SeedReport(setName, completed, false, $"{step.Name}: {ex.Message}");
            }

            completed.Add(step.Name);
        }

        logger.LogInformation("Seed set '{Set}' completed ({Count} steps).", setName, completed.Count);
        return new SeedReport(setName, completed, true);
    }

    /// <summary>
    /// Applies a schema script unless it is already recorded as applied. Returns false when
    /// it was skipped, which the caller reports so a re-run is legible rather than silent.
    /// </summary>
    private async Task<bool> ApplySchemaScriptOnceAsync(string scriptName, CancellationToken ct)
    {
        using var connection = dapperContext.CreateConnection();
        if (connection.State != ConnectionState.Open) connection.Open();

        await SchemaScriptHistory.EnsureTableAsync(connection, ct);
        if (await SchemaScriptHistory.IsAppliedAsync(connection, scriptName, ct))
        {
            logger.LogInformation("Schema script already applied, skipping: {Script}", scriptName);
            return false;
        }

        await SqlSeedScripts.ExecuteAsync(dapperContext, "Scripts", scriptName, ct);
        await SchemaScriptHistory.RecordAsync(connection, scriptName, ct);
        return true;
    }

    private async Task RunStepAsync(SeedStep step, CancellationToken ct)
    {
        switch (step.Kind)
        {
            case SeedStepKind.EfMigrations:
                // Identity tables are owned by EF Core exclusively; no SQL script creates them.
                var db = services.GetRequiredService<AppIdentityDbContext>();
                await db.Database.MigrateAsync(ct);
                break;

            case SeedStepKind.IdentityData:
                await IdentitySeeder.SeedAsync(services);
                break;

            case SeedStepKind.SqlSchema:
                await ApplySchemaScriptOnceAsync(step.Script!, ct);
                break;

            case SeedStepKind.SqlSeed:
                await SqlSeedScripts.ExecuteAsync(dapperContext, "Seeds", step.Script!, ct);
                break;

            default:
                throw new NotSupportedException($"Unhandled seed step kind: {step.Kind}");
        }
    }
}
