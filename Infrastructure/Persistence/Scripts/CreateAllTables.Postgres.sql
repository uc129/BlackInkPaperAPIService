-- PostgreSQL Migration Script for BlackInkPaperAPIService

-- -- 1. Identity Tables
-- CREATE TABLE IF NOT EXISTS Roles (
--     Id VARCHAR(450) PRIMARY KEY,
--     Name VARCHAR(256),
--     NormalizedName VARCHAR(256),
--     ConcurrencyStamp TEXT
-- );
-- 
-- CREATE TABLE IF NOT EXISTS Users (
--     Id VARCHAR(450) PRIMARY KEY,
--     FullName TEXT,
--     ArtistPortfolioUrl TEXT,
--     UserName VARCHAR(256),
--     NormalizedUserName VARCHAR(256),
--     Email VARCHAR(256),
--     NormalizedEmail VARCHAR(256),
--     EmailConfirmed BOOLEAN NOT NULL DEFAULT FALSE,
--     PasswordHash TEXT,
--     SecurityStamp TEXT,
--     ConcurrencyStamp TEXT,
--     PhoneNumber TEXT,
--     PhoneNumberConfirmed BOOLEAN NOT NULL DEFAULT FALSE,
--     TwoFactorEnabled BOOLEAN NOT NULL DEFAULT FALSE,
--     LockoutEnd TIMESTAMPTZ,
--     LockoutEnabled BOOLEAN NOT NULL DEFAULT FALSE,
--     AccessFailedCount INTEGER NOT NULL DEFAULT 0
-- );
-- 
-- CREATE TABLE IF NOT EXISTS AspNetRoleClaims (
--     Id SERIAL PRIMARY KEY,
--     RoleId VARCHAR(450) NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
--     ClaimType TEXT,
--     ClaimValue TEXT
-- );
-- 
-- CREATE TABLE IF NOT EXISTS AspNetUserClaims (
--     Id SERIAL PRIMARY KEY,
--     UserId VARCHAR(450) NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
--     ClaimType TEXT,
--     ClaimValue TEXT
-- );
-- 
-- CREATE TABLE IF NOT EXISTS AspNetUserLogins (
--     LoginProvider VARCHAR(450) NOT NULL,
--     ProviderKey VARCHAR(450) NOT NULL,
--     ProviderDisplayName TEXT,
--     UserId VARCHAR(450) NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
--     PRIMARY KEY (LoginProvider, ProviderKey)
-- );
-- 
-- CREATE TABLE IF NOT EXISTS AspNetUserRoles (
--     UserId VARCHAR(450) NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
--     RoleId VARCHAR(450) NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
--     PRIMARY KEY (UserId, RoleId)
-- );
-- 
-- CREATE TABLE IF NOT EXISTS AspNetUserTokens (
--     UserId VARCHAR(450) NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
--     LoginProvider VARCHAR(450) NOT NULL,
--     Name VARCHAR(450) NOT NULL,
--     Value TEXT,
--     PRIMARY KEY (UserId, LoginProvider, Name)
-- );

-- 2. Catalog Tables
CREATE TABLE IF NOT EXISTS ArtistProfiles (
    Id SERIAL PRIMARY KEY,
    UserId VARCHAR(450) NOT NULL,
    DisplayName VARCHAR(200) NOT NULL,
    Bio TEXT,
    ProfileImageUrl VARCHAR(1000),
    CoverImageUrl VARCHAR(1000),
    InstagramUrl VARCHAR(500),
    PortfolioUrl VARCHAR(500),
    WebsiteUrl VARCHAR(500),
    IsVerified BOOLEAN NOT NULL DEFAULT FALSE,
    TotalSales INTEGER NOT NULL DEFAULT 0,
    JoinedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ProductCategories (
    Id SERIAL PRIMARY KEY,
    NameCode VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    PrintName VARCHAR(200) NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsFeatured BOOLEAN NOT NULL DEFAULT FALSE,
    Slug VARCHAR(200) NOT NULL UNIQUE,
    CoverImageUrl VARCHAR(1000)
);

CREATE TABLE IF NOT EXISTS ProductSubCategories (
    Id SERIAL PRIMARY KEY,
    CategoryId INTEGER NOT NULL REFERENCES ProductCategories(Id) ON DELETE CASCADE,
    NameCode VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    PrintName VARCHAR(200) NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsFeatured BOOLEAN NOT NULL DEFAULT FALSE,
    Slug VARCHAR(200) NOT NULL UNIQUE,
    CoverImageUrl VARCHAR(1000)
);

CREATE TABLE IF NOT EXISTS ProductTags (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    Color VARCHAR(50),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ProductStandardVariants (
    Id SERIAL PRIMARY KEY,
    Label VARCHAR(100) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS ProductStandardVariantOptions (
    Id SERIAL PRIMARY KEY,
    StandardVariantId INTEGER NOT NULL REFERENCES ProductStandardVariants(Id) ON DELETE CASCADE,
    OptionName VARCHAR(100) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- 3. Product Tables
CREATE TABLE IF NOT EXISTS Products (
    Id SERIAL PRIMARY KEY,
    ArtistId INTEGER NOT NULL,
    ProductId VARCHAR(100) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) NOT NULL UNIQUE,
    NameCode VARCHAR(100) NOT NULL,
    PrintName VARCHAR(200) NOT NULL,
    Description TEXT,
    ShortDescription VARCHAR(1000),
    BasePrice DECIMAL(18,2) NOT NULL,
    FinalPrice DECIMAL(18,2) NOT NULL,
    CurrencyCode VARCHAR(10) NOT NULL DEFAULT 'INR',
    CategoryId INTEGER NOT NULL REFERENCES ProductCategories(Id),
    SubCategoryId INTEGER NOT NULL REFERENCES ProductSubCategories(Id),
    IsFeatured BOOLEAN NOT NULL DEFAULT FALSE,
    IsAvailable BOOLEAN NOT NULL DEFAULT TRUE,
    CoverImageUrl VARCHAR(1000),
    HeaderImageUrl VARCHAR(1000),
    AverageRating DOUBLE PRECISION NOT NULL DEFAULT 0,
    ReviewCount INTEGER NOT NULL DEFAULT 0,
    StockQuantity INTEGER,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(200),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(200),
    IsUsingStandardVariants BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS ArtSpecifications (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    Width DOUBLE PRECISION,
    Height DOUBLE PRECISION,
    Unit VARCHAR(20),
    WeightGrams INTEGER,
    IsFramed BOOLEAN,
    Material VARCHAR(200),
    FileFormat VARCHAR(50),
    ResolutionDpi INTEGER,
    PixelDimensions VARCHAR(100),
    PaperType VARCHAR(200),
    PaperWeight VARCHAR(100),
    InkType VARCHAR(200),
    IsOriginal BOOLEAN NOT NULL DEFAULT FALSE,
    IsSigned BOOLEAN NOT NULL DEFAULT FALSE,
    HasCertificate BOOLEAN NOT NULL DEFAULT FALSE,
    FramingStatus VARCHAR(100),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ProductImages (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    AltText VARCHAR(255) NOT NULL DEFAULT '',
    IsPrimary BOOLEAN NOT NULL DEFAULT FALSE,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    PublicId VARCHAR(100) NOT NULL DEFAULT '',
    BaseUrl VARCHAR(2048) NOT NULL DEFAULT '',
    AspectRatio DOUBLE PRECISION NOT NULL,
    Width INTEGER NOT NULL,
    Height INTEGER NOT NULL,
    PlaceholderUrl VARCHAR(2048),
    Format VARCHAR(50),
    Dpi INTEGER,
    FileSize BIGINT
);

CREATE TABLE IF NOT EXISTS Map_ProductTags (
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    TagId INTEGER NOT NULL REFERENCES ProductTags(Id) ON DELETE CASCADE,
    PRIMARY KEY (ProductId, TagId)
);

CREATE TABLE IF NOT EXISTS ProductVariants (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    Label VARCHAR(100) NOT NULL,
    FulfillmentType INTEGER NOT NULL, -- 0: Digital, 1: Physical
    Sku VARCHAR(100) NOT NULL,
    WeightGrams DECIMAL(18,2),
    StockQuantity INTEGER,
    AbsolutePrice DECIMAL(18,2),
    ProductImageId INTEGER REFERENCES ProductImages(Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS ProductVariantOptions (
    Id SERIAL PRIMARY KEY,
    ProductVariantId INTEGER NOT NULL REFERENCES ProductVariants(Id) ON DELETE CASCADE,
    Value VARCHAR(255) NOT NULL,
    PriceModifier DECIMAL(18,2) DEFAULT 0,
    AbsolutePrice DECIMAL(18,2),
    StockQuantity INTEGER
);

-- 4. Commerce Tables
CREATE TABLE IF NOT EXISTS ShippingAddresses (
    Id SERIAL PRIMARY KEY,
    UserId VARCHAR(450) NOT NULL,
    FullName VARCHAR(200) NOT NULL,
    PhoneNumber VARCHAR(50) NOT NULL,
    AddressLine1 VARCHAR(300) NOT NULL,
    AddressLine2 VARCHAR(300),
    City VARCHAR(100) NOT NULL,
    State VARCHAR(100) NOT NULL,
    PostalCode VARCHAR(20) NOT NULL,
    CountryCode VARCHAR(10) NOT NULL,
    Landmark VARCHAR(200),
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Orders (
    Id SERIAL PRIMARY KEY,
    OrderNumber VARCHAR(50) NOT NULL UNIQUE,
    UserId VARCHAR(450) NOT NULL,
    ShippingAddressId INTEGER NOT NULL REFERENCES ShippingAddresses(Id),
    CurrencyCode VARCHAR(10) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    PaymentStatus VARCHAR(50) NOT NULL DEFAULT 'Pending',
    PaymentProvider VARCHAR(50),
    RazorpayOrderId VARCHAR(100) UNIQUE,
    RazorpayPaymentId VARCHAR(100),
    RazorpaySignature VARCHAR(256),
    PaymentMethod VARCHAR(50),
    PaidAt TIMESTAMPTZ,
    PaymentFailureReason TEXT,
    Subtotal DECIMAL(18,2) NOT NULL,
    ShippingAmount DECIMAL(18,2) NOT NULL,
    ShippingMethod VARCHAR(100),
    ShippingLabel VARCHAR(200),
    TaxAmount DECIMAL(18,2) NOT NULL,
    TaxLabel VARCHAR(100),
    TaxRatePercent DECIMAL(9,4),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Notes TEXT,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS OrderItems (
    Id SERIAL PRIMARY KEY,
    OrderId INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    ProductDbId INTEGER NOT NULL,
    ProductId VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) NOT NULL,
    CoverImageUrl VARCHAR(1000),
    CurrencyCode VARCHAR(10) NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INTEGER NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    Sku VARCHAR(100),
    FulfillmentType VARCHAR(32)
);

CREATE TABLE IF NOT EXISTS OrderItemSelectedVariants (
    Id SERIAL PRIMARY KEY,
    OrderItemId INTEGER NOT NULL REFERENCES OrderItems(Id) ON DELETE CASCADE,
    ProductVariantId INTEGER NOT NULL,
    ProductVariantOptionId INTEGER NOT NULL,
    VariantLabel VARCHAR(100) NOT NULL,
    OptionValue VARCHAR(100) NOT NULL,
    PriceModifier DECIMAL(18,2),
    AbsolutePrice DECIMAL(18,2),
    Sku VARCHAR(100),
    FulfillmentType VARCHAR(32)
);

CREATE TABLE IF NOT EXISTS InventoryTransactions (
    Id SERIAL PRIMARY KEY,
    OrderId INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    ProductDbId INTEGER NOT NULL,
    ProductVariantOptionId INTEGER,
    QuantityChange INTEGER NOT NULL,
    Reason VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS PaymentWebhookEvents (
    Id SERIAL PRIMARY KEY,
    Provider VARCHAR(50) NOT NULL,
    EventId VARCHAR(200) NOT NULL,
    EventName VARCHAR(100) NOT NULL,
    ProcessedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (Provider, EventId)
);

CREATE TABLE IF NOT EXISTS Carts (
    Id SERIAL PRIMARY KEY,
    UserId VARCHAR(450) NOT NULL,
    CurrencyCode VARCHAR(10) NOT NULL,
    Status VARCHAR(32) NOT NULL DEFAULT 'Active',
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS CartItems (
    Id SERIAL PRIMARY KEY,
    CartId INTEGER NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
    ProductDbId INTEGER NOT NULL,
    ProductId VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) NOT NULL,
    CoverImageUrl VARCHAR(1000),
    CurrencyCode VARCHAR(10) NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INTEGER NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    Sku VARCHAR(100),
    FulfillmentType VARCHAR(32),
    AddedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS CartItemSelectedVariants (
    Id SERIAL PRIMARY KEY,
    CartItemId INTEGER NOT NULL REFERENCES CartItems(Id) ON DELETE CASCADE,
    ProductVariantId INTEGER NOT NULL,
    ProductVariantOptionId INTEGER NOT NULL,
    VariantLabel VARCHAR(100) NOT NULL,
    OptionValue VARCHAR(100) NOT NULL,
    PriceModifier DECIMAL(18,2),
    AbsolutePrice DECIMAL(18,2),
    Sku VARCHAR(100),
    FulfillmentType VARCHAR(32)
);

-- 5. Misc
CREATE TABLE IF NOT EXISTS TokenBlacklist (
    TokenId VARCHAR(450) PRIMARY KEY,
    Expiry TIMESTAMPTZ NOT NULL
);

-- Indexes
CREATE INDEX IX_Users_NormalizedUserName ON Users(NormalizedUserName);
CREATE INDEX IX_Users_NormalizedEmail ON Users(NormalizedEmail);
CREATE INDEX IX_ArtistProfiles_UserId ON ArtistProfiles(UserId);
CREATE INDEX IX_ProductCategories_Slug ON ProductCategories(Slug);
CREATE INDEX IX_ProductSubCategories_Slug ON ProductSubCategories(Slug);
CREATE INDEX IX_Products_Slug ON Products(Slug);
CREATE INDEX IX_Products_ProductId ON Products(ProductId);
CREATE INDEX IX_ProductImages_ProductId ON ProductImages(ProductId);
CREATE INDEX IX_ProductVariants_ProductId ON ProductVariants(ProductId);
CREATE INDEX IX_ShippingAddresses_UserId ON ShippingAddresses(UserId);
CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_RazorpayOrderId ON Orders(RazorpayOrderId);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_Carts_UserId_Status ON Carts(UserId, Status);
CREATE INDEX IX_CartItems_CartId ON CartItems(CartId);
CREATE INDEX IX_InventoryTransactions_OrderId ON InventoryTransactions(OrderId);
