namespace Infrastructure.Persistence.Seeding;

internal enum SeedStepKind
{
    /// <summary>Applies EF Core migrations. Identity tables are created this way and no other.</summary>
    EfMigrations,

    /// <summary>A .sql file under Persistence/Scripts — business table DDL.</summary>
    SqlSchema,

    /// <summary>Roles and users, created through UserManager so passwords are hashed properly.</summary>
    IdentityData,

    /// <summary>A .sql file under Persistence/Seeds — data.</summary>
    SqlSeed,
}

internal sealed record SeedStep(string Name, SeedStepKind Kind, string? Script = null);

/// <summary>
/// The one place that declares what each seed set contains. Adding a seed means adding a
/// step here — not another endpoint, and not another script-runner.
///
/// Schema scripts are listed explicitly rather than globbed: CreateAllTables must run
/// before the dated migrations, and filename sorting puts it last.
/// </summary>
public static class SeedSets
{
    public const string Schema = "schema";
    public const string Identity = "identity";
    public const string Catalog = "catalog";
    public const string Artwork = "artwork";
    public const string All = "all";

    private static readonly SeedStep Migrations = new("EF Core migrations (identity tables)", SeedStepKind.EfMigrations);

    private static readonly SeedStep[] SchemaSteps =
    [
        new("Business tables", SeedStepKind.SqlSchema, "CreateAllTables.Postgres.sql"),
        new("Image metadata and variant link", SeedStepKind.SqlSchema, "20260430_AddImageMetadataAndVariantLink.sql"),
        new("Audit logs and refresh tokens", SeedStepKind.SqlSchema, "20260508_AddAuditLogsAndRefreshTokens.sql"),
        new("Originals/Prints restructure", SeedStepKind.SqlSchema, "20260826_RestructureCategoriesOriginalsPrints.sql"),
        new("Contact submissions", SeedStepKind.SqlSchema, "20260830_AddContactSubmissions.sql"),
        new("One art-specification row per product", SeedStepKind.SqlSchema, "20260831_UniqueArtSpecificationsPerProduct.sql"),
    ];

    private static readonly SeedStep Users = new("Roles and users", SeedStepKind.IdentityData);
    private static readonly SeedStep Catalogue = new("Reference data and sample catalogue",
        SeedStepKind.SqlSeed, "SeedAll.Postgres.sql");
    private static readonly SeedStep RiaArtwork = new("Ria Mukharjee artwork catalogue",
        SeedStepKind.SqlSeed, "SeedRiaArtwork.sql");

    private static SeedStep[] Through(params SeedStep[] tail) => [Migrations, .. SchemaSteps, .. tail];

    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<SeedStep>> Definitions =
        new Dictionary<string, IReadOnlyList<SeedStep>>(StringComparer.OrdinalIgnoreCase)
        {
            [Schema] = Through(),
            [Identity] = Through(Users),
            [Catalog] = Through(Users, Catalogue),
            [Artwork] = Through(Users, Catalogue, RiaArtwork),
            [All] = Through(Users, Catalogue, RiaArtwork),
        };
}
