SET XACT_ABORT ON;
BEGIN TRANSACTION;





IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products
    (
        Id,
        ArtistId,
        ProductId,
        Name,
        Slug,
        NameCode,
        PrintName,
        Description,
        ShortDescription,
        BasePrice,
        FinalPrice,
        CurrencyCode,
        CategoryId,
        SubCategoryId,
        IsFeatured,
        IsAvailable,
        CoverImageUrl,
        HeaderImageUrl,
        AverageRating,
        ReviewCount,
        StockQuantity,
        CreatedAt,
        CreatedBy,
        UpdatedAt,
        UpdatedBy,
        ArtSpecId,
        IsUsingStandardVariants
    )
    VALUES
        (1, 1, 'BIP-ABS-001', 'Silent Geometry', 'silent-geometry', 'SILENT_GEOMETRY', 'Silent Geometry', 'A textured abstract composition designed for statement walls.', 'Abstract statement print.', 2499.00, 2199.00, 'INR', 1, 1, 1, 1, 'https://cdn.example.com/products/silent-geometry/cover.jpg', 'https://cdn.example.com/products/silent-geometry/header.jpg', 4.8, 12, 15, DATEADD(DAY, -14, SYSUTCDATETIME()), 'seed', SYSUTCDATETIME(), 'seed', 1, 1),
        (2, 1, 'BIP-DGT-002', 'Botanical Study Pack', 'botanical-study-pack', 'BOTANICAL_STUDY_PACK', 'Botanical Study Pack', 'A digital botanical poster set prepared for home and studio printing.', 'Printable botanical poster set.', 1299.00, 999.00, 'INR', 2, 2, 0, 1, 'https://cdn.example.com/products/botanical-study-pack/cover.jpg', 'https://cdn.example.com/products/botanical-study-pack/header.jpg', 4.5, 6, NULL, DATEADD(DAY, -10, SYSUTCDATETIME()), 'seed', SYSUTCDATETIME(), 'seed', 2, 1);
    SET IDENTITY_INSERT dbo.Products OFF;
END

IF OBJECT_ID('dbo.ArtSpecifications', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ArtSpecifications', 'ResolutionDpi') IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT dbo.ArtSpecifications OFF;
        INSERT INTO dbo.ArtSpecifications
        (
            ProductId,
            Width,
            Height,
            Unit,
            WeightGrams,
            IsFramed,
            Material,
            FileFormat,
            ResolutionDpi,
            PixelDimensions
        )
        VALUES
            (1, 42.0, 59.4, 'cm', 850, 1, 'Cotton Rag Paper', NULL, NULL, NULL),
            (2, NULL, NULL, 'cm', NULL, NULL, NULL, 'PDF', 300, '4961x7016');
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT dbo.ArtSpecifications ON;
        INSERT INTO dbo.ArtSpecifications
        (
            Id,
            ProductId,
            PaperType,
            PaperWeight,
            InkType,
            Height,
            Width,
            Unit,
            IsOriginal,
            IsSigned,
            HasCertificate,
            FramingStatus,
            UpdatedAt
        )
        VALUES
            (1, 1, 'Cotton Rag', '310 GSM', 'Archival Pigment', 59.4, 42.0, 'cm', 0, 1, 1, 'Framed', SYSUTCDATETIME()),
            (2, 2, 'Digital', NULL, NULL, NULL, NULL, 'cm', 0, 0, 0, 'Unframed', SYSUTCDATETIME());
        SET IDENTITY_INSERT dbo.ArtSpecifications OFF;
    END
END



IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ProductVariants', 'StandardVariantId') IS NOT NULL
    BEGIN
        INSERT INTO dbo.ProductVariants ( ProductId, Label, DisplayOrder, StandardVariantId)
        VALUES
            ( 1, 'Size', 1, 1),
            (2, 'Format', 1, 2);
    END
    
END

-- 1. Drop the table if it already exists
-- Note: If other tables have Foreign Keys pointing HERE, 
-- you must drop those constraints or tables first.
IF OBJECT_ID('dbo.ProductVariantOptions', 'U') IS NOT NULL
    DROP TABLE blackinkpaperDB.dbo.ProductVariantOptions;

-- 2. Create the table with the new schema
CREATE TABLE dbo.ProductVariantOptions (
                                                           Id INT IDENTITY(1,1) PRIMARY KEY,       -- Auto-incrementing unique identifier
                                                           ProductVariantId INT NOT NULL,          -- Link to the parent Product Variant
                                                           [Value] NVARCHAR(255) NOT NULL,         -- The option value (e.g., 'A4', 'Wooden Frame')
                                                           PriceModifier DECIMAL(18, 2) DEFAULT 0, -- Relative price change (e.g., +50.00)
                                                           AbsolutePrice DECIMAL(18, 2) NULL,      -- Set a specific price regardless of base
                                                           StockQuantity INT DEFAULT 0,            -- Inventory tracking for this specific option
                                                           FulfillmentType NVARCHAR(50) NULL,      -- e.g., 'Digital Download', 'Physical Ship'
                                                           Sku NVARCHAR(100) NULL,                 -- Unique Stock Keeping Unit for this variant
                                                           WeightGrams INT NULL,                   -- Shipping weight

    -- Optional: Add a Foreign Key constraint if the ProductVariants table exists
    -- CONSTRAINT FK_VariantOptions_Variant FOREIGN KEY (ProductVariantId) 
    -- REFERENCES blackinkpaperDB.dbo.ProductVariants(Id)
);

IF OBJECT_ID('dbo.ProductVariantOptions', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ProductVariantOptions', 'Value') IS NOT NULL
    BEGIN
        INSERT INTO dbo.ProductVariantOptions
        (
            ProductVariantId,
            Value,
            PriceModifier,
            AbsolutePrice,
            StockQuantity,
            FulfillmentType,
            Sku,
            WeightGrams
        )
        VALUES
            (1, 'A4 Print', 0.00, 2199.00, 10, 1, 'SG-A4-PRINT', 500),
            (1, 'A3 Print', 600.00, 2799.00, 5, 1, 'SG-A3-PRINT', 850),
            (2, 'Digital Download', -200.00, 999.00, NULL, 0, 'BSP-DIGITAL', NULL),
            (2, 'Fine Art Print', 400.00, 1599.00, 25, 1, 'BSP-PRINT', 250);
    END

-- 
-- IF OBJECT_ID('dbo.Map_ProductTags', 'U') IS NOT NULL
-- BEGIN
--     IF COL_LENGTH('dbo.Map_ProductTags', 'ProductTagId') IS NOT NULL
--     BEGIN
--         INSERT INTO dbo.Map_ProductTags (ProductId, TagId)
--         VALUES (1, 1), (1, 2), (2, 3);
--     END
--     ELSE
--     BEGIN
--         INSERT INTO dbo.Map_ProductTags (ProductId, TagId)
--         VALUES (1, 1), (1, 2), (2, 3);
--     END
-- END

IF OBJECT_ID('dbo.ShippingAddresses', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.ShippingAddresses ON;
    INSERT INTO dbo.ShippingAddresses
    (
        Id,
        UserId,
        FullName,
        PhoneNumber,
        AddressLine1,
        AddressLine2,
        City,
        State,
        PostalCode,
        CountryCode,
        Landmark,
        IsDefault,
        CreatedAt,
        UpdatedAt
    )
    VALUES
        (1, 'user-customer', 'Mia Sharma', '+910000000003', '221 Residency Road', 'Floor 3', 'Bengaluru', 'Karnataka', '560025', 'IN', 'Near Brigade Road', 1, DATEADD(DAY, -5, SYSUTCDATETIME()), SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.ShippingAddresses OFF;
END

IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.Orders ON;
    INSERT INTO dbo.Orders
    (
        Id,
        OrderNumber,
        UserId,
        ShippingAddressId,
        CurrencyCode,
        Status,
        PaymentStatus,
        PaymentProvider,
        RazorpayOrderId,
        RazorpayPaymentId,
        RazorpaySignature,
        PaymentMethod,
        PaidAt,
        PaymentFailureReason,
        Subtotal,
        ShippingAmount,
        ShippingMethod,
        ShippingLabel,
        TaxAmount,
        TaxLabel,
        TaxRatePercent,
        TotalAmount,
        Notes,
        CreatedAt,
        UpdatedAt
    )
    VALUES
        (1, 'ORD-2026-0001', 'user-customer', 1, 'INR', 'Confirmed', 'Paid', 'Razorpay', 'order_seed_001', 'pay_seed_001', 'sig_seed_001', 'UPI', DATEADD(DAY, -3, SYSUTCDATETIME()), NULL, 2199.00, 150.00, 'Standard', 'Standard Delivery', 110.00, 'GST', 5.0000, 2459.00, 'Seed order for local testing.', DATEADD(DAY, -3, SYSUTCDATETIME()), SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.Orders OFF;
END

IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.OrderItems ON;
    INSERT INTO dbo.OrderItems
    (
        Id,
        OrderId,
        ProductDbId,
        ProductId,
        Name,
        Slug,
        CoverImageUrl,
        CurrencyCode,
        BasePrice,
        UnitPrice,
        Quantity,
        LineTotal,
        Sku,
        FulfillmentType
    )
    VALUES
        (1, 1, 1, 'BIP-ABS-001', 'Silent Geometry', 'silent-geometry', 'https://cdn.example.com/products/silent-geometry/cover.jpg', 'INR', 2499.00, 2199.00, 1, 2199.00, 'SG-A4-PRINT', 'physical');
    SET IDENTITY_INSERT dbo.OrderItems OFF;
END

IF OBJECT_ID('dbo.OrderItemSelectedVariants', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.OrderItemSelectedVariants ON;
    INSERT INTO dbo.OrderItemSelectedVariants
    (
        Id,
        OrderItemId,
        ProductVariantId,
        ProductVariantOptionId,
        VariantLabel,
        OptionValue,
        PriceModifier,
        AbsolutePrice,
        Sku,
        FulfillmentType
    )
    VALUES
        (1, 1, 1, 1, 'Size', 'A4 Print', 0.00, 2199.00, 'SG-A4-PRINT', 'physical');
    SET IDENTITY_INSERT dbo.OrderItemSelectedVariants OFF;
END

IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.InventoryTransactions ON;
    INSERT INTO dbo.InventoryTransactions
    (
        Id,
        OrderId,
        ProductDbId,
        ProductVariantOptionId,
        QuantityChange,
        Reason,
        CreatedAt
    )
    VALUES
        (1, 1, 1, 1, -1, 'OrderPlaced', DATEADD(DAY, -3, SYSUTCDATETIME()));
    SET IDENTITY_INSERT dbo.InventoryTransactions OFF;
END

IF OBJECT_ID('dbo.PaymentWebhookEvents', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.PaymentWebhookEvents ON;
    INSERT INTO dbo.PaymentWebhookEvents (Id, Provider, EventId, EventName, ProcessedAt)
    VALUES (1, 'Razorpay', 'evt_seed_001', 'payment.captured', DATEADD(DAY, -3, SYSUTCDATETIME()));
    SET IDENTITY_INSERT dbo.PaymentWebhookEvents OFF;
END

IF OBJECT_ID('dbo.Carts', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.Carts ON;
    INSERT INTO dbo.Carts (Id, UserId, CurrencyCode, Status, CreatedAt, UpdatedAt)
    VALUES (1, 'user-customer', 'INR', 'Active', DATEADD(HOUR, -6, SYSUTCDATETIME()), SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.Carts OFF;
END

IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.CartItems ON;
    INSERT INTO dbo.CartItems
    (
        Id,
        CartId,
        ProductDbId,
        ProductId,
        Name,
        Slug,
        CoverImageUrl,
        CurrencyCode,
        BasePrice,
        UnitPrice,
        Quantity,
        LineTotal,
        Sku,
        FulfillmentType,
        AddedAt,
        UpdatedAt
    )
    VALUES
        (1, 1, 2, 'BIP-DGT-002', 'Botanical Study Pack', 'botanical-study-pack', 'https://cdn.example.com/products/botanical-study-pack/cover.jpg', 'INR', 1299.00, 999.00, 1, 999.00, 'BSP-DIGITAL', 'digital', DATEADD(HOUR, -6, SYSUTCDATETIME()), SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.CartItems OFF;
END

IF OBJECT_ID('dbo.CartItemSelectedVariants', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.CartItemSelectedVariants ON;
    INSERT INTO dbo.CartItemSelectedVariants
    (
        Id,
        CartItemId,
        ProductVariantId,
        ProductVariantOptionId,
        VariantLabel,
        OptionValue,
        PriceModifier,
        AbsolutePrice,
        Sku,
        FulfillmentType
    )
    VALUES
        (1, 1, 2, 3, 'Format', 'Digital Download', -200.00, 999.00, 'BSP-DIGITAL', 'digital');
    SET IDENTITY_INSERT dbo.CartItemSelectedVariants OFF;
END
END
COMMIT TRANSACTION;
