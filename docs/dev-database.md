# Development Database

Production and development are separate.

| Environment | Database | Config file |
|---|---|---|
| **Production** | Supabase (`aws-1-ap-south-1.pooler.supabase.com`) | `BlackInkPaperAPIService/appsettings.json` |
| **Development** | Local Postgres 16 in Docker, `localhost:5433` | `BlackInkPaperAPIService/appsettings.Development.json` |

Before this split, `appsettings.Development.json` pointed at the same Supabase project as
production, so running the API locally read and wrote live data. `appsettings.json` is
untouched and still deploys to Azure exactly as before.

## Where the SQL lives

| Path | Contents |
|---|---|
| `Infrastructure/Migrations/` | EF Core migrations. **Identity tables only** (`Users`, `Roles`, `AspNetUser*`) |
| `Infrastructure/Persistence/Scripts/` | Business table DDL and dated one-time migrations |
| `Infrastructure/Persistence/Seeds/` | Data — reference catalogue and Ria's artwork |
| `Infrastructure/Persistence/Seeding/` | The C# that runs all of the above |

Identity tables are created by EF Core and nothing else. No script in `Scripts/` may contain
identity DDL.

## Setup — one command

```bash
docker run -d --name blackinkpaper-dev-db \
  -e POSTGRES_USER=bip_dev -e POSTGRES_PASSWORD=bip_dev_local -e POSTGRES_DB=blackinkpaper_dev \
  -p 5433:5432 -v blackinkpaper-dev-pgdata:/var/lib/postgresql/data \
  --restart unless-stopped postgres:16

cd BlackInkPaperAPIService && ASPNETCORE_ENVIRONMENT=Development dotnet run -- --seed
```

That takes a completely empty database to a fully populated one: EF migrations, business
schema, roles and users, reference catalogue, and Ria's artwork. No manual `dotnet ef` step
and no `psql` needed.

## Seeding

One component — `ISeedRunner` — backs both entry points, so the CLI and the HTTP API can
never drift apart.

**Command line** (exit code 0 on success, 1 on failure, so CI can gate on it):

```bash
dotnet run -- --seed              # the "all" set
dotnet run -- --seed=identity     # just roles and users
```

**HTTP** (Development only — every action 404s otherwise):

```bash
# The API redirects http -> https, so use the https endpoint (launch profile "https").
curl -k https://localhost:7023/api/seed             # list the sets
curl -k -X POST https://localhost:7023/api/seed/all
```

**Manual SQL** — every script under `Scripts/` and `Seeds/` still runs standalone:

```bash
docker cp Infrastructure/Persistence/Seeds/SeedRiaArtwork.sql blackinkpaper-dev-db:/tmp/
docker exec blackinkpaper-dev-db psql -U bip_dev -d blackinkpaper_dev \
  -v ON_ERROR_STOP=1 -f /tmp/SeedRiaArtwork.sql
```

### The sets

| Set | Contents |
|---|---|
| `schema` | EF migrations + business tables |
| `identity` | …plus roles, `admin@admin.com` / `Admin@123!`, `artist@artist.com` / `Artist@123!` |
| `catalog` | …plus reference data and the sample catalogue |
| `artwork` / `all` | …plus Ria's 25 artworks as 50 products |

Sets are declared in one place, `Persistence/Seeding/SeedSets.cs`. Adding a seed means adding
a step there — not another endpoint and not another script-runner.

### Re-running is safe

Seeding is idempotent; running `all` three times leaves the same row counts. Two mechanisms:

- **Schema scripts run once**, tracked in a `SchemaScriptHistory` table (the same idea as
  `__EFMigrationsHistory`). This matters because the dated scripts are one-time data
  migrations — `20260826_RestructureCategoriesOriginalsPrints` rewrites the old taxonomy, and
  re-applying it to an already-restructured database would corrupt it.
- **Seed data is guarded** — `ON CONFLICT DO NOTHING` in the reference seed, `IF NOT EXISTS`
  on slug and SKU in the artwork seed.

To start over:

```bash
docker exec blackinkpaper-dev-db psql -U bip_dev -d blackinkpaper_dev \
  -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
cd BlackInkPaperAPIService && ASPNETCORE_ENVIRONMENT=Development dotnet run -- --seed
```

## Ria's artwork catalogue

`Seeds/SeedRiaArtwork.sql` is generated, not hand-written:

```bash
CLOUDINARY_CLOUD_NAME=... CLOUDINARY_API_KEY=... CLOUDINARY_API_SECRET=... \
  python3 upload_artwork_to_cloudinary.py     # uploads to the blackinkpaper_art folder
python3 generate_artwork_seed.py              # reads cloudinary_artwork_results.json
```

Before running it against **production**:

- The production `ArtistProfiles` row must already read `Ria Mukharjee`. The script raises
  and applies nothing otherwise, so it cannot credit the catalogue to the wrong artist.
- Products are seeded as **drafts** (`IsAvailable = FALSE`) with placeholder prices
  (₹15,000 originals; the existing ₹1,499/₹1,199 template for prints). Set real prices in the
  admin portal and flip availability per piece.
- Physical dimensions are left NULL, not guessed.
- It adds two sub-categories under Originals — Architecture and Sculpture Studies — because
  Commissions was the only existing one and does not describe this work.
- No hardcoded IDs: every insert uses `RETURNING`, categories resolve by `NameCode`, and the
  whole thing is one transaction.

The seed runner is Development-only, so production gets these by hand — and it needs the
two newer schema scripts first (`contact_submissions` does not exist in production yet):

```bash
for f in Infrastructure/Persistence/Scripts/20260830_AddContactSubmissions.sql \
         Infrastructure/Persistence/Scripts/20260831_UniqueArtSpecificationsPerProduct.sql \
         Infrastructure/Persistence/Seeds/SeedRiaArtwork.sql; do
  docker cp "$f" blackinkpaper-dev-db:/tmp/
  docker exec blackinkpaper-dev-db psql "$PROD_CONNECTION_STRING" \
    -v ON_ERROR_STOP=1 -f "/tmp/$(basename "$f")"
done
```

## Secrets

The SendGrid key is no longer in `appsettings.json`. Supply it out-of-band:

```bash
export SendGrid__ApiKey="SG.…"      # an Azure App Setting in production
```

Unset, the API registers `StubEmailService` and logs mail instead of sending it, so local
development works without a key. **Create the Azure App Setting before deploying a blanked
`appsettings.json`**, or production ships without a key.

The key that was previously committed is in git history and must be rotated.

## Which photo is which

`docs/artwork-catalog-proposal.json` records how the 74 source photographs group into the 25
artworks, including the process and duplicate shots that were deliberately *not* uploaded.
It is the grouping record, not an upload manifest — `cloudinary_artwork_results.json` is what
actually went to Cloudinary.

## Everyday use

```bash
docker start blackinkpaper-dev-db
docker exec -it blackinkpaper-dev-db psql -U bip_dev -d blackinkpaper_dev
docker rm -f blackinkpaper-dev-db && docker volume rm blackinkpaper-dev-pgdata
```

The host has no `psql`; use `docker exec` as above. Verify the API is on dev and not prod —
`/health` includes a Postgres probe:

```bash
cd BlackInkPaperAPIService && ASPNETCORE_ENVIRONMENT=Development dotnet run
curl http://localhost:5004/health
```
