SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.ArtistProfiles', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ArtistProfiles ON;
    INSERT INTO dbo.ArtistProfiles
    (
        Id,
        UserId,
        DisplayName,
        Bio,
        ProfileImageUrl,
        CoverImageUrl,
        InstagramUrl,
        PortfolioUrl,
        WebsiteUrl,
        IsVerified,
        TotalSales,
        JoinedAt,
        UpdatedAt
    )
    VALUES
        (1, 'user-artist', 'Aarav Kapoor', 'Mixed media and digital print artist.', 'https://cdn.example.com/artists/aarav/profile.jpg', 'https://cdn.example.com/artists/aarav/cover.jpg', 'https://instagram.com/aaravkapoorart', 'https://portfolio.blackinkpaper.local/aarav', 'https://aaravkapoor.art', 1, 42, DATEADD(DAY, -180, SYSUTCDATETIME()), SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.ArtistProfiles OFF;
END

IF OBJECT_ID('dbo.ProductCategories', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ProductCategories ON;
    INSERT INTO dbo.ProductCategories
    (
        Id,
        NameCode,
        Name,
        PrintName,
        Description,
        IsActive,
        IsFeatured,
        Slug,
        CoverImageUrl,
        CreatedAt
    )
    VALUES
        (1, 'WALL_ART', 'Wall Art', 'Wall Art', 'Framed and unframed wall pieces.', 1, 1, 'wall-art', 'https://cdn.example.com/categories/wall-art.jpg', SYSUTCDATETIME()),
        (2, 'DIGITAL_PRINTS', 'Digital Prints', 'Digital Prints', 'Downloadable art and print-ready assets.', 1, 0, 'digital-prints', 'https://cdn.example.com/categories/digital-prints.jpg', SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.ProductCategories OFF;
END

IF OBJECT_ID('dbo.ProductSubCategories', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ProductSubCategories ON;
    INSERT INTO dbo.ProductSubCategories
    (
        Id,
        CategoryId,
        NameCode,
        Name,
        PrintName,
        Description,
        IsActive,
        IsFeatured,
        Slug,
        CoverImageUrl,
        CreatedAt
    )
    VALUES
        (1, 1, 'ABSTRACT', 'Abstract', 'Abstract', 'Abstract wall art collection.', 1, 1, 'abstract', 'https://cdn.example.com/subcategories/abstract.jpg', SYSUTCDATETIME()),
        (2, 1, 'BOTANICAL', 'Botanical', 'Botanical', 'Botanical themed wall art.', 1, 0, 'botanical', 'https://cdn.example.com/subcategories/botanical.jpg', SYSUTCDATETIME()),
        (3, 2, 'POSTERS', 'Posters', 'Posters', 'Printable posters and digital posters.', 1, 0, 'posters', 'https://cdn.example.com/subcategories/posters.jpg', SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.ProductSubCategories OFF;
END

IF OBJECT_ID('dbo.ProductTags', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ProductTags ON;
    INSERT INTO dbo.ProductTags (Id, Name, Slug, Color, CreatedAt)
    VALUES
        (1, 'Bestseller', 'bestseller', '#C97B2C', SYSUTCDATETIME()),
        (2, 'Limited Edition', 'limited-edition', '#7A3E2B', SYSUTCDATETIME()),
        (3, 'New Arrival', 'new-arrival', '#2F7C5F', SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.ProductTags OFF;
END

IF OBJECT_ID('dbo.ProductStandardVariants', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ProductStandardVariants ON;
    INSERT INTO dbo.ProductStandardVariants (Id, Label, IsActive)
    VALUES
        (1, 'Size', 1),
        (2, 'Format', 1);
    SET IDENTITY_INSERT dbo.ProductStandardVariants OFF;
END

IF OBJECT_ID('dbo.ProductStandardVariantOptions', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ProductStandardVariantOptions ON;
    INSERT INTO dbo.ProductStandardVariantOptions (Id, StandardVariantId, OptionName, IsActive)
    VALUES
        (1, 1, 'A4', 1),
        (2, 1, 'A3', 1),
        (3, 2, 'Digital Download', 1),
        (4, 2, 'Fine Art Print', 1);
    SET IDENTITY_INSERT dbo.ProductStandardVariantOptions OFF;
END

COMMIT TRANSACTION;
