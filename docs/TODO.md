# Outstanding work

Last updated: 2026-08-31

## Secrets — do before the next deploy to `main`

CI/CD deploys the API on push to `main`, and `appsettings.json` no longer carries the
SendGrid key. If the Azure App Setting is not there first, production ships without a key
and silently falls back to `StubEmailService` — mail stops sending, with no error.

- [ ] **Add `SendGrid__ApiKey` to Azure App Settings** (double underscore — that is how .NET
      maps an environment variable onto `SendGrid:ApiKey`). *Assigned: Utkarsh.*
- [ ] **Rotate the SendGrid key.** The old one was committed and is still in git history;
      blanking the file does not unring that. Generate a new key in SendGrid, put the new one
      in Azure, then revoke the old.

### Other credentials still committed in `appsettings.json`

These predate this work and remain in git history. Rotating them means updating Azure App
Settings and, for the database, the connection strings:

- [ ] Supabase database password (`ConnectionStrings:DefaultConnection`)
- [ ] Cloudinary API secret (also hardcoded in `upload_illustrations.sh` and
      `upload_remaining.sh` — move those to env vars like `upload_artwork_to_cloudinary.py`)
- [ ] Azure SQL password (`ConnectionStrings:DefaultConnectionSQLSERVER`)
- [ ] JWT signing key (`Jwt:Key`) — rotating this invalidates every issued token

## Artwork catalogue — 50 products are live but hidden

Seeded to production on 2026-08-31: 25 artworks, each as an Original (stock 1) and a Print.
All are drafts (`IsAvailable = FALSE`), so nothing is customer-visible yet.

- [ ] **Set real prices.** Placeholders are ₹15,000 for originals and the inherited
      ₹1,499 / ₹1,199 template for prints. Nobody chose these numbers.
- [ ] **Publish** each piece by flipping `IsAvailable` once its price is right.
- [ ] **Fill in physical dimensions** — left NULL rather than guessed. Also paper type,
      paper weight, ink, framing status, signed / certificate flags.
- [ ] **Have Ria confirm the 25 titles.** They are inferred from photographs, and they are
      baked into Cloudinary public_ids and product slugs. Slugs are still free to change
      while the products are drafts. See `docs/artwork-catalog-proposal.json`.
- [ ] **Settle the surname spelling.** The signature on several pieces reads
      "R. Mukherjee" (with an *e*); the codebase uses "Mukharjee" throughout, as specified.

## Production database

- [ ] **Decide a migration policy.** Prod had silently drifted: it was missing
      `20260430_AddImageMetadataAndVariantLink.sql`, which only surfaced when the artwork
      seed failed on a missing `productimages.format` column. The dev seed runner tracks
      applied scripts in `SchemaScriptHistory`; production has no equivalent, so nothing
      records what has and has not been applied there.
- [ ] Consider creating `SchemaScriptHistory` in production and backfilling it with the
      scripts known to be applied, so the same drift cannot recur silently.

## Housekeeping

- [ ] **Storefront repo has uncommitted changes** and is not covered by this commit:
      `web-apps/frontend-apps/Next.Js/BlackinPaperStore/blackinkpaper-store` — the persona
      rewrite (4 components) and the regenerated OpenAPI spec.
- [ ] **Rename or remove the production Supabase data source in Rider.** It sits in the same
      data-source list as the local one, so a mis-click edits live data.
- [ ] The 74 source photographs live in `wwwroot/assets/blackinkpaper-art` and are
      gitignored by design — they are delivered via Cloudinary. Keep a backup elsewhere;
      the repo is not one.
