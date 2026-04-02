IF OBJECT_ID('dbo.PaymentWebhookEvents', 'U') IS NOT NULL
    DROP TABLE dbo.PaymentWebhookEvents;
GO

IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NOT NULL
    DROP TABLE dbo.InventoryTransactions;
GO

IF OBJECT_ID('dbo.OrderItemSelectedVariants', 'U') IS NOT NULL
    DROP TABLE dbo.OrderItemSelectedVariants;
GO

IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL
    DROP TABLE dbo.OrderItems;
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL
    DROP TABLE dbo.Orders;
GO

IF OBJECT_ID('dbo.ShippingAddresses', 'U') IS NOT NULL
    DROP TABLE dbo.ShippingAddresses;
GO

CREATE TABLE dbo.ShippingAddresses
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(50) NOT NULL,
    AddressLine1 NVARCHAR(300) NOT NULL,
    AddressLine2 NVARCHAR(300) NULL,
    City NVARCHAR(100) NOT NULL,
    State NVARCHAR(100) NOT NULL,
    PostalCode NVARCHAR(20) NOT NULL,
    CountryCode NVARCHAR(10) NOT NULL,
    Landmark NVARCHAR(200) NULL,
    IsDefault BIT NOT NULL CONSTRAINT DF_ShippingAddresses_IsDefault DEFAULT (0),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
GO

CREATE INDEX IX_ShippingAddresses_UserId ON dbo.ShippingAddresses(UserId);
GO

CREATE TABLE dbo.Orders
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderNumber NVARCHAR(50) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    ShippingAddressId INT NOT NULL,
    CurrencyCode NVARCHAR(10) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    PaymentStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_PaymentStatus DEFAULT ('Pending'),
    PaymentProvider NVARCHAR(50) NULL,
    RazorpayOrderId NVARCHAR(100) NULL,
    RazorpayPaymentId NVARCHAR(100) NULL,
    RazorpaySignature NVARCHAR(256) NULL,
    PaymentMethod NVARCHAR(50) NULL,
    PaidAt DATETIME2 NULL,
    PaymentFailureReason NVARCHAR(1000) NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    ShippingAmount DECIMAL(18,2) NOT NULL,
    ShippingMethod NVARCHAR(100) NULL,
    ShippingLabel NVARCHAR(200) NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    TaxLabel NVARCHAR(100) NULL,
    TaxRatePercent DECIMAL(9,4) NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber),
    CONSTRAINT UQ_Orders_RazorpayOrderId UNIQUE (RazorpayOrderId),
    CONSTRAINT FK_Orders_ShippingAddresses FOREIGN KEY (ShippingAddressId) REFERENCES dbo.ShippingAddresses(Id)
);
GO

CREATE INDEX IX_Orders_UserId ON dbo.Orders(UserId);
CREATE INDEX IX_Orders_RazorpayOrderId ON dbo.Orders(RazorpayOrderId);
GO

CREATE TABLE dbo.OrderItems
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
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
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
GO

CREATE TABLE dbo.OrderItemSelectedVariants
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderItemId INT NOT NULL,
    ProductVariantId INT NOT NULL,
    ProductVariantOptionId INT NOT NULL,
    VariantLabel NVARCHAR(100) NOT NULL,
    OptionValue NVARCHAR(100) NOT NULL,
    PriceModifier DECIMAL(18,2) NULL,
    AbsolutePrice DECIMAL(18,2) NULL,
    Sku NVARCHAR(100) NULL,
    FulfillmentType NVARCHAR(32) NULL,
    CONSTRAINT FK_OrderItemSelectedVariants_OrderItems FOREIGN KEY (OrderItemId) REFERENCES dbo.OrderItems(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_OrderItemSelectedVariants_OrderItemId ON dbo.OrderItemSelectedVariants(OrderItemId);
GO

CREATE TABLE dbo.InventoryTransactions
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductDbId INT NOT NULL,
    ProductVariantOptionId INT NULL,
    QuantityChange INT NOT NULL,
    Reason NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_InventoryTransactions_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_InventoryTransactions_OrderId ON dbo.InventoryTransactions(OrderId);
CREATE INDEX IX_InventoryTransactions_ProductDbId ON dbo.InventoryTransactions(ProductDbId);
GO

CREATE TABLE dbo.PaymentWebhookEvents
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Provider NVARCHAR(50) NOT NULL,
    EventId NVARCHAR(200) NOT NULL,
    EventName NVARCHAR(100) NOT NULL,
    ProcessedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_PaymentWebhookEvents UNIQUE (Provider, EventId)
);
GO
