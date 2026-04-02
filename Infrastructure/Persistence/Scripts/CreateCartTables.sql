IF OBJECT_ID('dbo.CartItemSelectedVariants', 'U') IS NOT NULL
    DROP TABLE dbo.CartItemSelectedVariants;
GO

IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL
    DROP TABLE dbo.CartItems;
GO

IF OBJECT_ID('dbo.Carts', 'U') IS NOT NULL
    DROP TABLE dbo.Carts;
GO

CREATE TABLE dbo.Carts
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    CurrencyCode NVARCHAR(10) NOT NULL,
    Status NVARCHAR(32) NOT NULL CONSTRAINT DF_Carts_Status DEFAULT ('Active'),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
GO

CREATE INDEX IX_Carts_UserId_Status ON dbo.Carts(UserId, Status);
GO

CREATE TABLE dbo.CartItems
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CartId INT NOT NULL,
    ProductDbId INT NOT NULL,
    ProductId NVARCHAR(100) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(200) NOT NULL,
    CoverImageUrl NVARCHAR(1000) NULL,
    CurrencyCode NVARCHAR(10) NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    Sku NVARCHAR(100) NULL,
    FulfillmentType NVARCHAR(32) NULL,
    AddedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_CartItems_Carts FOREIGN KEY (CartId) REFERENCES dbo.Carts(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_CartItems_CartId ON dbo.CartItems(CartId);
GO

CREATE TABLE dbo.CartItemSelectedVariants
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CartItemId INT NOT NULL,
    ProductVariantId INT NOT NULL,
    ProductVariantOptionId INT NOT NULL,
    VariantLabel NVARCHAR(100) NOT NULL,
    OptionValue NVARCHAR(100) NOT NULL,
    PriceModifier DECIMAL(18,2) NULL,
    AbsolutePrice DECIMAL(18,2) NULL,
    Sku NVARCHAR(100) NULL,
    FulfillmentType NVARCHAR(32) NULL,
    CONSTRAINT FK_CartItemSelectedVariants_CartItems FOREIGN KEY (CartItemId) REFERENCES dbo.CartItems(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_CartItemSelectedVariants_CartItemId ON dbo.CartItemSelectedVariants(CartItemId);
GO
