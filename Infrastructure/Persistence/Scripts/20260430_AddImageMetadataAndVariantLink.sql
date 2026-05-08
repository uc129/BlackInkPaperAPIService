-- Migration: Add Metadata to ProductImages and Link ProductVariants to Images
-- Date: 2026-04-30

ALTER TABLE ProductImages 
ADD COLUMN IF NOT EXISTS Format VARCHAR(50),
ADD COLUMN IF NOT EXISTS Dpi INTEGER,
ADD COLUMN IF NOT EXISTS FileSize BIGINT;

ALTER TABLE ProductVariants
ADD COLUMN IF NOT EXISTS ProductImageId INTEGER REFERENCES ProductImages(Id) ON DELETE SET NULL;
