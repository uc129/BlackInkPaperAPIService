-- 1. Drop the table if it exists
IF OBJECT_ID('blackinkpaperDB.dbo.ProductImages', 'U') IS NOT NULL
    DROP TABLE blackinkpaperDB.dbo.ProductImages;

-- 2. Create the table based on your C# Model
CREATE TABLE blackinkpaperDB.dbo.ProductImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,      -- Maps to: public int Id
    ProductId INT NOT NULL,                -- Maps to: public int ProductId
    AltText NVARCHAR(255) NOT NULL         -- Maps to: public string AltText
        DEFAULT '', 
    IsPrimary BIT NOT NULL                 -- Maps to: public bool IsPrimary
        DEFAULT 0,
    DisplayOrder INT NOT NULL              -- Maps to: public int DisplayOrder
        DEFAULT 0,
    PublicId NVARCHAR(100) NOT NULL        -- Maps to: public string PublicId (e.g., Cloudinary ID)
        DEFAULT '',
    BaseUrl NVARCHAR(2048) NOT NULL        -- Maps to: public string BaseUrl
        DEFAULT '',
    AspectRatio FLOAT NOT NULL,            -- Maps to: public double AspectRatio
    Width INT NOT NULL,                    -- Maps to: public int Width
    Height INT NOT NULL,                   -- Maps to: public int Height
    PlaceholderUrl NVARCHAR(2048) NULL     -- Maps to: public string? PlaceholderUrl (Nullable)
);

-- 3. Optimization: Index the ProductId
-- Since you will almost always query images by ProductId, this is essential.
CREATE INDEX IX_ProductImages_ProductId 
ON blackinkpaperDB.dbo.ProductImages (ProductId);