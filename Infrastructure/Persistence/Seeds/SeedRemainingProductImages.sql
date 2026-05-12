-- Auto-generated: additional product images from remaining Cloudinary uploads

INSERT INTO ProductImages
(Id, ProductId, AltText, IsPrimary, DisplayOrder, PublicId, BaseUrl, AspectRatio, Width, Height, PlaceholderUrl)
OVERRIDING SYSTEM VALUE
VALUES
(21, 7, 'Asset 32', FALSE, 2, 'illustrations/asset_32', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778613925/illustrations/asset_32.svg', 0.67, 398, 594, NULL),
(22, 4, 'Asset 33', FALSE, 2, 'illustrations/asset_33', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618133/illustrations/asset_33.svg', 0.8873, 362, 408, NULL),
(23, 12, 'Asset 34', FALSE, 2, 'illustrations/asset_34', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618162/illustrations/asset_34.svg', 1.0476, 440, 420, NULL),
(24, 11, 'Asset 35', FALSE, 2, 'illustrations/asset_35', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618219/illustrations/asset_35.svg', 0.6875, 440, 640, NULL),
(25, 11, 'Asset 36', FALSE, 3, 'illustrations/asset_36', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618250/illustrations/asset_36.svg', 0.6603, 309, 468, NULL),
(26, 8, 'Asset 37', FALSE, 2, 'illustrations/asset_37', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618263/illustrations/asset_37.svg', 0.7958, 382, 480, NULL),
(27, 7, 'Asset 38', FALSE, 3, 'illustrations/asset_38', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618280/illustrations/asset_38.svg', 0.683, 405, 593, NULL),
(28, 21, 'Asset 39', FALSE, 2, 'illustrations/asset_39', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618305/illustrations/asset_39.svg', 0.6577, 390, 593, NULL),
(29, 6, 'Asset 4', FALSE, 2, 'illustrations/asset_4', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618319/illustrations/asset_4.svg', 0.9662, 372, 385, NULL),
(30, 22, 'Asset 40', FALSE, 2, 'illustrations/asset_40', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618337/illustrations/asset_40.svg', 1.0216, 236, 231, NULL),
(31, 17, 'Asset 41', FALSE, 2, 'illustrations/asset_41', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618378/illustrations/asset_41.svg', 0.9066, 427, 471, NULL),
(32, 5, 'Asset 42', FALSE, 2, 'illustrations/asset_42', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618407/illustrations/asset_42.svg', 0.6444, 386, 599, NULL),
(33, 4, 'Asset 43', FALSE, 3, 'illustrations/asset_43', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618449/illustrations/asset_43.svg', 0.7566, 460, 608, NULL),
(34, 6, 'Asset 44', FALSE, 3, 'illustrations/asset_44', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618495/illustrations/asset_44.svg', 0.6631, 370, 558, NULL),
(35, 10, 'Asset 45', FALSE, 2, 'illustrations/asset_45', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618508/illustrations/asset_45.svg', 1.2874, 318, 247, NULL),
(36, 11, 'Asset 46', FALSE, 4, 'illustrations/asset_46', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618542/illustrations/asset_46.svg', 0.729, 417, 572, NULL),
(37, 20, 'Asset 5', FALSE, 2, 'illustrations/asset_5', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618569/illustrations/asset_5.svg', 1.9091, 378, 198, NULL),
(38, 23, 'Asset 6', FALSE, 2, 'illustrations/asset_6', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618587/illustrations/asset_6.svg', 0.7199, 239, 332, NULL),
(39, 4, 'Asset 7', FALSE, 4, 'illustrations/asset_7', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618614/illustrations/asset_7.svg', 0.7163, 447, 624, NULL),
(40, 21, 'Asset 8', FALSE, 3, 'illustrations/asset_8', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618631/illustrations/asset_8.svg', 1.3526, 844, 624, NULL),
(41, 10, 'Asset 9', FALSE, 3, 'illustrations/asset_9', 'https://res.cloudinary.com/blackinkpaper/image/upload/v1778618705/illustrations/asset_9.svg', 0.6683, 417, 624, NULL);

SELECT setval(pg_get_serial_sequence('productimages', 'id'), COALESCE(MAX(id), 1)) FROM productimages;