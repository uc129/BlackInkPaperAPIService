-- One-time data migration: restructure the LIVE product taxonomy into the two
-- top-level categories Originals + Prints, keeping the existing catalogue as
-- sub-categories of Prints.
--
-- Observed live structure (4 categories, each with 2 sub-categories):
--   1 Black & White   -> 1 Monochrome Portraits, 2 Abstract B&W
--   2 Cityscapes      -> 3 Urban Scenes,         4 Architectural Studies
--   3 Commissions     -> 5 Custom Portraits,     6 Pet Portraits
--   4 Travel Art      -> 7 Wanderlust Prints,    8 Cultural Scenes
--
-- Target (2 levels only):
--   Originals (1)  -> Commissions (sub 5)
--   Prints    (2)  -> Black & White (1), Cityscapes (3), Travel Art (7)
--
-- Each product keeps its theme: its old top-level category becomes a
-- sub-category. Commissions (one-off custom/pet portraits) is treated as an
-- original, so it lands under Originals; the rest sit under Prints. The finer
-- 8 sub-categories collapse into that parent.
--
-- Run once, manually, against the live database (Supabase SQL editor or psql).
-- Re-running is a no-op. This targets the ids listed above; if the live ids
-- differ, stop and re-check before running.

BEGIN;

-- 1. Repurpose the two kept top-level categories in place.
UPDATE ProductCategories
SET NameCode = 'ORIGINALS', Name = 'Originals', PrintName = 'Originals',
    Slug = 'originals',
    Description = 'One-of-a-kind original artworks — a single available piece per artwork.',
    IsActive = TRUE, IsFeatured = TRUE
WHERE Id = 1;

UPDATE ProductCategories
SET NameCode = 'PRINTS', Name = 'Prints', PrintName = 'Prints',
    Slug = 'prints',
    Description = 'Fine art prints and digital downloads.',
    IsActive = TRUE, IsFeatured = TRUE
WHERE Id = 2;

-- 2. Re-point every product by its old THEME (top category, identified here by
--    its old sub-category) to the kept representative sub: Commissions moves to
--    Originals (1), the rest to Prints (2). Keyed on the stable old
--    sub-category ids so nothing double-processes.
UPDATE Products SET CategoryId = 2, SubCategoryId = 1 WHERE SubCategoryId IN (1, 2); -- Black & White -> Prints
UPDATE Products SET CategoryId = 2, SubCategoryId = 3 WHERE SubCategoryId IN (3, 4); -- Cityscapes    -> Prints
UPDATE Products SET CategoryId = 1, SubCategoryId = 5 WHERE SubCategoryId IN (5, 6); -- Commissions   -> Originals
UPDATE Products SET CategoryId = 2, SubCategoryId = 7 WHERE SubCategoryId IN (7, 8); -- Travel Art    -> Prints

-- 3. Repurpose the kept sub-categories: 3 themes under Prints, Commissions
--    under Originals.
UPDATE ProductSubCategories
SET CategoryId = 2, NameCode = 'BLACK_AND_WHITE', Name = 'Black & White',
    PrintName = 'Black & White', Slug = 'black-and-white', IsActive = TRUE
WHERE Id = 1;

UPDATE ProductSubCategories
SET CategoryId = 2, NameCode = 'CITYSCAPES', Name = 'Cityscapes',
    PrintName = 'Cityscapes', Slug = 'cityscapes', IsActive = TRUE
WHERE Id = 3;

UPDATE ProductSubCategories
SET CategoryId = 1, NameCode = 'COMMISSIONS', Name = 'Commissions',
    PrintName = 'Commissions', Slug = 'commissions', IsActive = TRUE
WHERE Id = 5;

UPDATE ProductSubCategories
SET CategoryId = 2, NameCode = 'TRAVEL_ART', Name = 'Travel Art',
    PrintName = 'Travel Art', Slug = 'travel-art', IsActive = TRUE
WHERE Id = 7;

-- 4. Drop the now-unused finer sub-categories (no product references them).
DELETE FROM ProductSubCategories
WHERE Id IN (2, 4, 6, 8)
  AND NOT EXISTS (SELECT 1 FROM Products p WHERE p.SubCategoryId = ProductSubCategories.Id);

-- 5. Drop the now-unused legacy top categories (no sub-category or product references them).
DELETE FROM ProductCategories
WHERE Id IN (3, 4)
  AND NOT EXISTS (SELECT 1 FROM ProductSubCategories s WHERE s.CategoryId = ProductCategories.Id)
  AND NOT EXISTS (SELECT 1 FROM Products p WHERE p.CategoryId = ProductCategories.Id);

-- 6. Keep identity sequences ahead of the retained ids.
SELECT setval(pg_get_serial_sequence('ProductCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductCategories;
SELECT setval(pg_get_serial_sequence('ProductSubCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductSubCategories;

COMMIT;

-- Verification (run after the migration; Commissions under Originals, the rest under Prints):
SELECT c.Name AS category, s.Name AS subcategory, COUNT(p.Id) AS products
FROM ProductCategories c
LEFT JOIN ProductSubCategories s ON s.CategoryId = c.Id
LEFT JOIN Products p ON p.CategoryId = c.Id AND p.SubCategoryId = s.Id
GROUP BY c.Id, c.Name, s.Id, s.Name
ORDER BY c.Id, s.Id;
