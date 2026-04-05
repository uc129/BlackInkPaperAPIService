SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260326075858_InitialIdentity', '9.0.0');
END

IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES
        ('role-admin', 'Admin', 'ADMIN', 'role-admin-stamp'),
        ('role-artist', 'Artist', 'ARTIST', 'role-artist-stamp'),
        ('role-user', 'User', 'USER', 'role-user-stamp');
END

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.Users
    (
        Id,
        FullName,
        ArtistPortfolioUrl,
        UserName,
        NormalizedUserName,
        Email,
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumber,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnd,
        LockoutEnabled,
        AccessFailedCount
    )
    VALUES
        ('user-admin', 'System Administrator', NULL, 'admin@blackinkpaper.local', 'ADMIN@BLACKINKPAPER.LOCAL', 'admin@blackinkpaper.local', 'ADMIN@BLACKINKPAPER.LOCAL', 1, 'PLACEHOLDER_HASH_ADMIN', 'admin-security-stamp', 'admin-concurrency-stamp', '+910000000001', 1, 0, NULL, 0, 0),
        ('user-artist', 'Aarav Kapoor', 'https://portfolio.blackinkpaper.local/aarav', 'artist@blackinkpaper.local', 'ARTIST@BLACKINKPAPER.LOCAL', 'artist@blackinkpaper.local', 'ARTIST@BLACKINKPAPER.LOCAL', 1, 'PLACEHOLDER_HASH_ARTIST', 'artist-security-stamp', 'artist-concurrency-stamp', '+910000000002', 1, 0, NULL, 0, 0),
        ('user-customer', 'Mia Sharma', NULL, 'user@blackinkpaper.local', 'USER@BLACKINKPAPER.LOCAL', 'user@blackinkpaper.local', 'USER@BLACKINKPAPER.LOCAL', 1, 'PLACEHOLDER_HASH_USER', 'user-security-stamp', 'user-concurrency-stamp', '+910000000003', 1, 0, NULL, 0, 0);
END

IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES
        ('user-admin', 'role-admin'),
        ('user-artist', 'role-artist'),
        ('user-customer', 'role-user');
END

IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.AspNetRoleClaims ON;
    INSERT INTO dbo.AspNetRoleClaims (Id, RoleId, ClaimType, ClaimValue)
    VALUES
        (1, 'role-admin', 'permission', 'products:write'),
        (2, 'role-artist', 'permission', 'products:write-own'),
        (3, 'role-user', 'permission', 'checkout:read');
    SET IDENTITY_INSERT dbo.AspNetRoleClaims OFF;
END

IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.AspNetUserClaims ON;
    INSERT INTO dbo.AspNetUserClaims (Id, UserId, ClaimType, ClaimValue)
    VALUES
        (1, 'user-admin', 'display_name', 'System Administrator'),
        (2, 'user-artist', 'display_name', 'Aarav Kapoor'),
        (3, 'user-customer', 'display_name', 'Mia Sharma');
    SET IDENTITY_INSERT dbo.AspNetUserClaims OFF;
END

IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)
    VALUES ('SeedProvider', 'seed-artist-login', 'Seed Provider', 'user-artist');
END

IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AspNetUserTokens (UserId, LoginProvider, Name, Value)
    VALUES
        ('user-admin', 'SeedProvider', 'refresh_token', 'seed-refresh-admin'),
        ('user-artist', 'SeedProvider', 'refresh_token', 'seed-refresh-artist');
END

IF OBJECT_ID('dbo.TokenBlacklist', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.TokenBlacklist (TokenId, ExpiryDate)
    VALUES ('seed-revoked-token', DATEADD(DAY, 7, SYSUTCDATETIME()));
END

COMMIT TRANSACTION;
