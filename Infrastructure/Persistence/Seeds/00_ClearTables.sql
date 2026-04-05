SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CartItemSelectedVariants', 'U') IS NOT NULL DELETE FROM dbo.CartItemSelectedVariants;
IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL DELETE FROM dbo.CartItems;
IF OBJECT_ID('dbo.Carts', 'U') IS NOT NULL DELETE FROM dbo.Carts;

IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NOT NULL DELETE FROM dbo.InventoryTransactions;
IF OBJECT_ID('dbo.OrderItemSelectedVariants', 'U') IS NOT NULL DELETE FROM dbo.OrderItemSelectedVariants;
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DELETE FROM dbo.OrderItems;
IF OBJECT_ID('dbo.PaymentWebhookEvents', 'U') IS NOT NULL DELETE FROM dbo.PaymentWebhookEvents;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DELETE FROM dbo.Orders;
IF OBJECT_ID('dbo.ShippingAddresses', 'U') IS NOT NULL DELETE FROM dbo.ShippingAddresses;

IF OBJECT_ID('dbo.Map_ProductTags', 'U') IS NOT NULL DELETE FROM dbo.Map_ProductTags;
IF OBJECT_ID('dbo.ProductVariantOptions', 'U') IS NOT NULL DELETE FROM dbo.ProductVariantOptions;
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL DELETE FROM dbo.ProductVariants;
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL DELETE FROM dbo.ProductImages;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DELETE FROM dbo.Products;
IF OBJECT_ID('dbo.ArtSpecifications', 'U') IS NOT NULL DELETE FROM dbo.ArtSpecifications;
IF OBJECT_ID('dbo.ProductStandardVariantOptions', 'U') IS NOT NULL DELETE FROM dbo.ProductStandardVariantOptions;
IF OBJECT_ID('dbo.ProductStandardVariants', 'U') IS NOT NULL DELETE FROM dbo.ProductStandardVariants;
IF OBJECT_ID('dbo.ProductTags', 'U') IS NOT NULL DELETE FROM dbo.ProductTags;
IF OBJECT_ID('dbo.ProductSubCategories', 'U') IS NOT NULL DELETE FROM dbo.ProductSubCategories;
IF OBJECT_ID('dbo.ProductCategories', 'U') IS NOT NULL DELETE FROM dbo.ProductCategories;
IF OBJECT_ID('dbo.ArtistProfiles', 'U') IS NOT NULL DELETE FROM dbo.ArtistProfiles;

IF OBJECT_ID('dbo.TokenBlacklist', 'U') IS NOT NULL DELETE FROM dbo.TokenBlacklist;

IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserTokens;
IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserLogins;
IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserClaims;
IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL DELETE FROM dbo.AspNetRoleClaims;
IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NOT NULL DELETE FROM dbo.AspNetUserRoles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DELETE FROM dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DELETE FROM dbo.Roles;

IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL DELETE FROM dbo.__EFMigrationsHistory;

COMMIT TRANSACTION;

IF OBJECT_ID('dbo.ArtSpecifications', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ArtSpecifications'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ArtSpecifications', RESEED, 0);
IF OBJECT_ID('dbo.ArtistProfiles', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ArtistProfiles'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ArtistProfiles', RESEED, 0);
IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.AspNetRoleClaims'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.AspNetRoleClaims', RESEED, 0);
IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.AspNetUserClaims'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.AspNetUserClaims', RESEED, 0);
IF OBJECT_ID('dbo.CartItemSelectedVariants', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.CartItemSelectedVariants'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.CartItemSelectedVariants', RESEED, 0);
IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.CartItems'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.CartItems', RESEED, 0);
IF OBJECT_ID('dbo.Carts', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.Carts'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.Carts', RESEED, 0);
IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.InventoryTransactions'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.InventoryTransactions', RESEED, 0);
IF OBJECT_ID('dbo.OrderItemSelectedVariants', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.OrderItemSelectedVariants'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.OrderItemSelectedVariants', RESEED, 0);
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.OrderItems'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.OrderItems', RESEED, 0);
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.Orders'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.Orders', RESEED, 0);
IF OBJECT_ID('dbo.PaymentWebhookEvents', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.PaymentWebhookEvents'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.PaymentWebhookEvents', RESEED, 0);
IF OBJECT_ID('dbo.ProductCategories', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductCategories'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductCategories', RESEED, 0);
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductImages'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductImages', RESEED, 0);
IF OBJECT_ID('dbo.ProductStandardVariantOptions', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductStandardVariantOptions'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductStandardVariantOptions', RESEED, 0);
IF OBJECT_ID('dbo.ProductStandardVariants', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductStandardVariants'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductStandardVariants', RESEED, 0);
IF OBJECT_ID('dbo.ProductSubCategories', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductSubCategories'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductSubCategories', RESEED, 0);
IF OBJECT_ID('dbo.ProductTags', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductTags'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductTags', RESEED, 0);
IF OBJECT_ID('dbo.ProductVariantOptions', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductVariantOptions'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductVariantOptions', RESEED, 0);
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ProductVariants'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ProductVariants', RESEED, 0);
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.Products'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
IF OBJECT_ID('dbo.ShippingAddresses', 'U') IS NOT NULL AND OBJECTPROPERTY(OBJECT_ID('dbo.ShippingAddresses'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('dbo.ShippingAddresses', RESEED, 0);
