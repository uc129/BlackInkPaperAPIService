namespace Infrastructure.Persistence.Seeding;

/// <summary>
/// The single entry point for populating a database, shared by the `--seed` command line
/// and the /api/seed endpoints so both paths always do exactly the same thing.
/// </summary>
public interface ISeedRunner
{
    /// <summary>Names accepted by <see cref="RunAsync"/>.</summary>
    IReadOnlyList<string> AvailableSets { get; }

    Task<SeedReport> RunAsync(string setName, CancellationToken ct = default);
}

public sealed record SeedReport(string Set, IReadOnlyList<string> StepsRun, bool Success, string? Error = null);
