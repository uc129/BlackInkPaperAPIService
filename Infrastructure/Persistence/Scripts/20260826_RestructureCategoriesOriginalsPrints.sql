-- One-time data migration: restructure the product taxonomy into the two
-- top-level categories Originals + Prints, each with a single default
-- sub-category, and move every existing product under Prints.
--
-- Context:
--   The catalogue previously used categories Wall Art (1) / Digital Prints (2)
--   with sub-categories Abstract (1) / Botanical (2) / Posters (3). All live
--   products (the seeded pieces plus the CLI-uploaded illustrations) are
--   print / digital-variant products, so they all become Prints. Originals is
--   left empty, ready for genuine one-off pieces (a product whose
--   ArtSpecifications.IsOriginal = TRUE is routed to Originals automatically).
--
-- This targets an already-populated database and does NOT re-run the seed
-- scripts (those INSERT fixed ids and would collide). Run it once, manually,
-- against the live database (Supabase SQL editor or psql). It is idempotent:
-- re-running it is a no-op. Fixed ids match SeedAll.Postgres.sql so fresh and
-- migrated databases end up identical.

BEGIN;

-- 1. Repurpose the two top-level categories in place (id 1 = Originals, 2 = Prints).
INSERT INTO ProductCategories
(Id, NameCode, Name, PrintName, Description, IsActive, IsFeatured, Slug, CoverImageUrl)
VALUES
(1, 'ORIGINALS', 'Originals', 'Originals', 'One-of-a-kind original artworks — a single available piece per artwork.', TRUE, TRUE, 'originals', 'https://cdn.example.com/categories/originals.jpg'),
(2, 'PRINTS', 'Prints', 'Prints', 'Fine art prints and digital downloads.', TRUE, TRUE, 'prints', 'https://cdn.example.com/categories/prints.jpg')
ON CONFLICT (Id) DO UPDATE SET
    NameCode      = EXCLUDED.NameCode,
    Name          = EXCLUDED.Name,
    PrintName     = EXCLUDED.PrintName,
    Description   = EXCLUDED.Description,
    IsActive      = EXCLUDED.IsActive,
    IsFeatured    = EXCLUDED.IsFeatured,
    Slug          = EXCLUDED.Slug,
    CoverImageUrl = EXCLUDED.CoverImageUrl;

-- 2. Repurpose the default sub-categories (sub 1 = Originals under 1,
--    sub 2 = Prints under 2). Legacy subs 2 (Botanical) and 3 (Posters) are
--    collapsed away below.
INSERT INTO ProductSubCategories
(Id, CategoryId, NameCode, Name, PrintName, Description, IsActive, IsFeatured, Slug, CoverImageUrl)
VALUES
(1, 1, 'ORIGINALS_ALL', 'Originals', 'Originals', 'All original artworks.', TRUE, TRUE, 'originals-all', 'https://cdn.example.com/subcategories/originals-all.jpg'),
(2, 2, 'PRINTS_ALL', 'Prints', 'Prints', 'All prints and digital downloads.', TRUE, TRUE, 'prints-all', 'https://cdn.example.com/subcategories/prints-all.jpg')
ON CONFLICT (Id) DO UPDATE SET
    CategoryId    = EXCLUDED.CategoryId,
    NameCode      = EXCLUDED.NameCode,
    Name          = EXCLUDED.Name,
    PrintName     = EXCLUDED.PrintName,
    Description   = EXCLUDED.Description,
    IsActive      = EXCLUDED.IsActive,
    IsFeatured    = EXCLUDED.IsFeatured,
    Slug          = EXCLUDED.Slug,
    CoverImageUrl = EXCLUDED.CoverImageUrl;

-- 3. Route existing products: originals -> Originals (1/1), everything else -> Prints (2/2).
UPDATE Products p
SET CategoryId = 2, SubCategoryId = 2
WHERE NOT EXISTS (
    SELECT 1 FROM ArtSpecifications s
    WHERE s.ProductId = p.Id AND s.IsOriginal = TRUE
);

UPDATE Products p
SET CategoryId = 1, SubCategoryId = 1
WHERE EXISTS (
    SELECT 1 FROM ArtSpecifications s
    WHERE s.ProductId = p.Id AND s.IsOriginal = TRUE
);

-- 4. Drop the now-unused legacy sub-categories (anything beyond 1/2 that no
--    product references). Guarded so it never orphans a live product.
DELETE FROM ProductSubCategories sub
WHERE sub.Id NOT IN (1, 2)
  AND NOT EXISTS (SELECT 1 FROM Products p WHERE p.SubCategoryId = sub.Id);

-- 5. Keep identity sequences ahead of the fixed ids used above.
SELECT setval(pg_get_serial_sequence('ProductCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductCategories;
SELECT setval(pg_get_serial_sequence('ProductSubCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductSubCategories;

COMMIT;
