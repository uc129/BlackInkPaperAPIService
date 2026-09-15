-- ArtSpecifications is a per-product block: the domain, the DTOs and the admin form all
-- assume exactly one row per product. Nothing enforced it, so re-running a seed silently
-- accumulated duplicate spec rows and ON CONFLICT DO NOTHING had no key to match on.

-- Collapse any existing duplicates, keeping the earliest row per product.
DELETE FROM ArtSpecifications a
USING ArtSpecifications b
WHERE a.ProductId = b.ProductId
  AND a.Id > b.Id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_artspecifications_productid'
    ) THEN
        ALTER TABLE ArtSpecifications
            ADD CONSTRAINT uq_artspecifications_productid UNIQUE (ProductId);
    END IF;
END $$;
