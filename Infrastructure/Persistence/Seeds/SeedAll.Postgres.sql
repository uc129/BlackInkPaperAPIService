-- PostgreSQL Seed Script for BlackInkPaperAPIService

BEGIN;

-- 1. Seed ArtistProfiles
INSERT INTO ArtistProfiles
(Id, UserId, DisplayName, Bio, ProfileImageUrl, CoverImageUrl, InstagramUrl, PortfolioUrl, WebsiteUrl, IsVerified, TotalSales, JoinedAt, UpdatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'user-artist', 'Aarav Kapoor', 'Mixed media and digital print artist.', 'https://cdn.example.com/artists/aarav/profile.jpg', 'https://cdn.example.com/artists/aarav/cover.jpg', 'https://instagram.com/aaravkapoorart', 'https://portfolio.blackinkpaper.local/aarav', 'https://aaravkapoor.art', TRUE, 42, CURRENT_TIMESTAMP - INTERVAL '180 days', CURRENT_TIMESTAMP);

-- 2. Seed ProductCategories
INSERT INTO ProductCategories
(Id, NameCode, Name, PrintName, Description, IsActive, IsFeatured, Slug, CoverImageUrl)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'WALL_ART', 'Wall Art', 'Wall Art', 'Framed and unframed wall pieces.', TRUE, TRUE, 'wall-art', 'https://cdn.example.com/categories/wall-art.jpg'),
(2, 'DIGITAL_PRINTS', 'Digital Prints', 'Digital Prints', 'Downloadable art and print-ready assets.', TRUE, FALSE, 'digital-prints', 'https://cdn.example.com/categories/digital-prints.jpg');

-- 3. Seed ProductSubCategories
INSERT INTO ProductSubCategories
(Id, CategoryId, NameCode, Name, PrintName, Description, IsActive, IsFeatured, Slug, CoverImageUrl)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 'ABSTRACT', 'Abstract', 'Abstract', 'Abstract wall art collection.', TRUE, TRUE, 'abstract', 'https://cdn.example.com/subcategories/abstract.jpg'),
(2, 1, 'BOTANICAL', 'Botanical', 'Botanical', 'Botanical themed wall art.', TRUE, FALSE, 'botanical', 'https://cdn.example.com/subcategories/botanical.jpg'),
(3, 2, 'POSTERS', 'Posters', 'Posters', 'Printable posters and digital posters.', TRUE, FALSE, 'posters', 'https://cdn.example.com/subcategories/posters.jpg');

-- 4. Seed ProductTags
INSERT INTO ProductTags
(Id, Name, Slug, Color, CreatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'Bestseller', 'bestseller', '#C97B2C', CURRENT_TIMESTAMP),
(2, 'Limited Edition', 'limited-edition', '#7A3E2B', CURRENT_TIMESTAMP),
(3, 'New Arrival', 'new-arrival', '#2F7C5F', CURRENT_TIMESTAMP);

-- 5. Seed ProductStandardVariants
INSERT INTO ProductStandardVariants
(Id, Label, IsActive)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'Size', TRUE),
(2, 'Format', TRUE);

-- 6. Seed ProductStandardVariantOptions
INSERT INTO ProductStandardVariantOptions
(Id, StandardVariantId, OptionName, IsActive)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 'A4', TRUE),
(2, 1, 'A3', TRUE),
(3, 2, 'Digital Download', TRUE),
(4, 2, 'Fine Art Print', TRUE);

-- 7. Seed Products
INSERT INTO Products
(Id, ArtistId, ProductId, Name, Slug, NameCode, PrintName, Description, ShortDescription, BasePrice, FinalPrice, CurrencyCode, CategoryId, SubCategoryId, IsFeatured, IsAvailable, CoverImageUrl, HeaderImageUrl, AverageRating, ReviewCount, StockQuantity, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsUsingStandardVariants)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 'BIP-ABS-001', 'Silent Geometry', 'silent-geometry', 'SILENT_GEOMETRY', 'Silent Geometry', 'A textured abstract composition designed for statement walls.', 'Abstract statement print.', 2499.00, 2199.00, 'INR', 1, 1, TRUE, TRUE, 'https://cdn.example.com/products/silent-geometry/cover.jpg', 'https://cdn.example.com/products/silent-geometry/header.jpg', 4.8, 12, 15, CURRENT_TIMESTAMP - INTERVAL '14 days', 'seed', CURRENT_TIMESTAMP, 'seed', TRUE),
(2, 1, 'BIP-DGT-002', 'Botanical Study Pack', 'botanical-study-pack', 'BOTANICAL_STUDY_PACK', 'Botanical Study Pack', 'A digital botanical poster set prepared for home and studio printing.', 'Printable botanical poster set.', 1299.00, 999.00, 'INR', 2, 3, FALSE, TRUE, 'https://cdn.example.com/products/botanical-study-pack/cover.jpg', 'https://cdn.example.com/products/botanical-study-pack/header.jpg', 4.5, 6, NULL, CURRENT_TIMESTAMP - INTERVAL '10 days', 'seed', CURRENT_TIMESTAMP, 'seed', TRUE);

-- 8. Seed ArtSpecifications
INSERT INTO ArtSpecifications
(ProductId, Width, Height, Unit, WeightGrams, IsFramed, Material, FileFormat, ResolutionDpi, PixelDimensions, PaperType, PaperWeight, InkType, IsOriginal, IsSigned, HasCertificate, FramingStatus, CreatedAt, UpdatedAt)
VALUES
(1, 42.0, 59.4, 'cm', 850, TRUE, 'Cotton Rag Paper', NULL, NULL, NULL, 'Cotton Rag', '310 GSM', 'Archival Pigment', FALSE, TRUE, TRUE, 'Framed', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, NULL, NULL, 'cm', NULL, NULL, NULL, 'PDF', 300, '4961x7016', 'Digital', NULL, NULL, FALSE, FALSE, FALSE, 'Unframed', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- 9. Seed ProductVariants
INSERT INTO ProductVariants
(Id, ProductId, Label, FulfillmentType, Sku, WeightGrams, StockQuantity, AbsolutePrice)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 'A4 Physical Print', 1, 'SG-A4-PRINT', 500, 10, 2199.00),
(2, 1, 'A3 Physical Print', 1, 'SG-A3-PRINT', 850, 5, 2799.00),
(3, 2, 'Digital Download', 0, 'BSP-DIGITAL', 0, NULL, 999.00),
(4, 2, 'Fine Art Print', 1, 'BSP-PRINT', 250, 25, 1599.00);

-- 10. Seed ProductVariantOptions
INSERT INTO ProductVariantOptions
(Id, ProductVariantId, Value, PriceModifier, AbsolutePrice, StockQuantity)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 'A4', 0.00, 2199.00, 10),
(2, 2, 'A3', 600.00, 2799.00, 5),
(3, 3, 'Digital', 0.00, 999.00, NULL),
(4, 4, 'Physical', 400.00, 1599.00, 25);

-- 11. Seed Map_ProductTags
INSERT INTO Map_ProductTags (ProductId, TagId)
VALUES (1, 1), (1, 2), (2, 3);

-- 12. Seed ShippingAddresses
INSERT INTO ShippingAddresses
(Id, UserId, FullName, PhoneNumber, AddressLine1, AddressLine2, City, State, PostalCode, CountryCode, Landmark, IsDefault, CreatedAt, UpdatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'user-customer', 'Mia Sharma', '+910000000003', '221 Residency Road', 'Floor 3', 'Bengaluru', 'Karnataka', '560025', 'IN', 'Near Brigade Road', TRUE, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP);

-- 13. Seed Orders
INSERT INTO Orders
(Id, OrderNumber, UserId, ShippingAddressId, CurrencyCode, Status, PaymentStatus, PaymentProvider, RazorpayOrderId, RazorpayPaymentId, RazorpaySignature, PaymentMethod, PaidAt, PaymentFailureReason, Subtotal, ShippingAmount, ShippingMethod, ShippingLabel, TaxAmount, TaxLabel, TaxRatePercent, TotalAmount, Notes, CreatedAt, UpdatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 'ORD-2026-0001', 'user-customer', 1, 'INR', 'Paid', 'Captured', 'Razorpay', 'order_seed_001', 'pay_seed_001', 'sig_seed_001', 'UPI', CURRENT_TIMESTAMP - INTERVAL '3 days', NULL, 2199.00, 150.00, 'Standard', 'Standard Delivery', 110.00, 'GST', 5.0000, 2459.00, 'Seed order for local testing.', CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP);

-- 14. Seed OrderItems
INSERT INTO OrderItems
(Id, OrderId, ProductDbId, ProductId, Name, Slug, CoverImageUrl, CurrencyCode, BasePrice, UnitPrice, Quantity, LineTotal, Sku, FulfillmentType)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 1, 'BIP-ABS-001', 'Silent Geometry', 'silent-geometry', 'https://cdn.example.com/products/silent-geometry/cover.jpg', 'INR', 2499.00, 2199.00, 1, 2199.00, 'SG-A4-PRINT', 'physical');

-- 15. Seed OrderItemSelectedVariants
INSERT INTO OrderItemSelectedVariants
(Id, OrderItemId, ProductVariantId, ProductVariantOptionId, VariantLabel, OptionValue, PriceModifier, AbsolutePrice, Sku, FulfillmentType)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 1, 1, 'A4 Physical Print', 'A4', 0.00, 2199.00, 'SG-A4-PRINT', 'physical');

-- 16. Seed InventoryTransactions
INSERT INTO InventoryTransactions
(Id, OrderId, ProductDbId, ProductVariantOptionId, QuantityChange, Reason, CreatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 1, 1, -1, 'OrderPaymentCaptured', CURRENT_TIMESTAMP - INTERVAL '3 days');

-- 17. Seed PaymentWebhookEvents
INSERT INTO PaymentWebhookEvents (Id, Provider, EventId, EventName, ProcessedAt)
OVERRIDING SYSTEM VALUE
VALUES (1, 'Razorpay', 'evt_seed_001', 'payment.captured', CURRENT_TIMESTAMP - INTERVAL '3 days');

-- 18. Seed Carts
INSERT INTO Carts (Id, UserId, CurrencyCode, Status, CreatedAt, UpdatedAt)
OVERRIDING SYSTEM VALUE
VALUES (1, 'user-customer', 'INR', 'Active', CURRENT_TIMESTAMP - INTERVAL '6 hours', CURRENT_TIMESTAMP);

-- 19. Seed CartItems
INSERT INTO CartItems
(Id, CartId, ProductDbId, ProductId, Name, Slug, CoverImageUrl, CurrencyCode, BasePrice, UnitPrice, Quantity, LineTotal, Sku, FulfillmentType, AddedAt, UpdatedAt)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 2, 'BIP-DGT-002', 'Botanical Study Pack', 'botanical-study-pack', 'https://cdn.example.com/products/botanical-study-pack/cover.jpg', 'INR', 1299.00, 999.00, 1, 999.00, 'BSP-DIGITAL', 'digital', CURRENT_TIMESTAMP - INTERVAL '6 hours', CURRENT_TIMESTAMP);

-- 20. Seed CartItemSelectedVariants
INSERT INTO CartItemSelectedVariants
(Id, CartItemId, ProductVariantId, ProductVariantOptionId, VariantLabel, OptionValue, PriceModifier, AbsolutePrice, Sku, FulfillmentType)
OVERRIDING SYSTEM VALUE
VALUES
(1, 1, 3, 3, 'Digital Download', 'Digital', 0.00, 999.00, 'BSP-DIGITAL', 'digital');

-- Sync sequences
SELECT setval(pg_get_serial_sequence('ArtistProfiles', 'id'), COALESCE(MAX(id), 1)) FROM ArtistProfiles;
SELECT setval(pg_get_serial_sequence('ProductCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductCategories;
SELECT setval(pg_get_serial_sequence('ProductSubCategories', 'id'), COALESCE(MAX(id), 1)) FROM ProductSubCategories;
SELECT setval(pg_get_serial_sequence('ProductTags', 'id'), COALESCE(MAX(id), 1)) FROM ProductTags;
SELECT setval(pg_get_serial_sequence('ProductStandardVariants', 'id'), COALESCE(MAX(id), 1)) FROM ProductStandardVariants;
SELECT setval(pg_get_serial_sequence('ProductStandardVariantOptions', 'id'), COALESCE(MAX(id), 1)) FROM ProductStandardVariantOptions;
SELECT setval(pg_get_serial_sequence('Products', 'id'), COALESCE(MAX(id), 1)) FROM Products;
SELECT setval(pg_get_serial_sequence('ArtSpecifications', 'id'), COALESCE(MAX(id), 1)) FROM ArtSpecifications;
SELECT setval(pg_get_serial_sequence('ProductImages', 'id'), COALESCE(MAX(id), 1)) FROM ProductImages;
SELECT setval(pg_get_serial_sequence('ProductVariants', 'id'), COALESCE(MAX(id), 1)) FROM ProductVariants;
SELECT setval(pg_get_serial_sequence('ProductVariantOptions', 'id'), COALESCE(MAX(id), 1)) FROM ProductVariantOptions;
SELECT setval(pg_get_serial_sequence('ShippingAddresses', 'id'), COALESCE(MAX(id), 1)) FROM ShippingAddresses;
SELECT setval(pg_get_serial_sequence('Orders', 'id'), COALESCE(MAX(id), 1)) FROM Orders;
SELECT setval(pg_get_serial_sequence('OrderItems', 'id'), COALESCE(MAX(id), 1)) FROM OrderItems;
SELECT setval(pg_get_serial_sequence('OrderItemSelectedVariants', 'id'), COALESCE(MAX(id), 1)) FROM OrderItemSelectedVariants;
SELECT setval(pg_get_serial_sequence('InventoryTransactions', 'id'), COALESCE(MAX(id), 1)) FROM InventoryTransactions;
SELECT setval(pg_get_serial_sequence('PaymentWebhookEvents', 'id'), COALESCE(MAX(id), 1)) FROM PaymentWebhookEvents;
SELECT setval(pg_get_serial_sequence('Carts', 'id'), COALESCE(MAX(id), 1)) FROM Carts;
SELECT setval(pg_get_serial_sequence('CartItems', 'id'), COALESCE(MAX(id), 1)) FROM CartItems;
SELECT setval(pg_get_serial_sequence('CartItemSelectedVariants', 'id'), COALESCE(MAX(id), 1)) FROM CartItemSelectedVariants;

COMMIT;
