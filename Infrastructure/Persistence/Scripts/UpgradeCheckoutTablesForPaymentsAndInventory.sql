IF COL_LENGTH('dbo.Orders', 'PaymentStatus') IS NULL
    ALTER TABLE dbo.Orders ADD PaymentStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_PaymentStatus DEFAULT ('Pending');
GO

IF COL_LENGTH('dbo.Orders', 'PaymentProvider') IS NULL
    ALTER TABLE dbo.Orders ADD PaymentProvider NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RazorpayOrderId') IS NULL
    ALTER TABLE dbo.Orders ADD RazorpayOrderId NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RazorpayPaymentId') IS NULL
    ALTER TABLE dbo.Orders ADD RazorpayPaymentId NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RazorpaySignature') IS NULL
    ALTER TABLE dbo.Orders ADD RazorpaySignature NVARCHAR(256) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'PaymentMethod') IS NULL
    ALTER TABLE dbo.Orders ADD PaymentMethod NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'PaidAt') IS NULL
    ALTER TABLE dbo.Orders ADD PaidAt DATETIME2 NULL;
GO

IF COL_LENGTH('dbo.Orders', 'PaymentFailureReason') IS NULL
    ALTER TABLE dbo.Orders ADD PaymentFailureReason NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'ShippingMethod') IS NULL
    ALTER TABLE dbo.Orders ADD ShippingMethod NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'ShippingLabel') IS NULL
    ALTER TABLE dbo.Orders ADD ShippingLabel NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'TaxLabel') IS NULL
    ALTER TABLE dbo.Orders ADD TaxLabel NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'TaxRatePercent') IS NULL
    ALTER TABLE dbo.Orders ADD TaxRatePercent DECIMAL(9,4) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_RazorpayOrderId' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE INDEX IX_Orders_RazorpayOrderId ON dbo.Orders(RazorpayOrderId);
GO

IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NULL
BEGIN
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
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryTransactions_OrderId' AND object_id = OBJECT_ID('dbo.InventoryTransactions'))
    CREATE INDEX IX_InventoryTransactions_OrderId ON dbo.InventoryTransactions(OrderId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryTransactions_ProductDbId' AND object_id = OBJECT_ID('dbo.InventoryTransactions'))
    CREATE INDEX IX_InventoryTransactions_ProductDbId ON dbo.InventoryTransactions(ProductDbId);
GO

IF OBJECT_ID('dbo.PaymentWebhookEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentWebhookEvents
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Provider NVARCHAR(50) NOT NULL,
        EventId NVARCHAR(200) NOT NULL,
        EventName NVARCHAR(100) NOT NULL,
        ProcessedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_PaymentWebhookEvents UNIQUE (Provider, EventId)
    );
END
GO
