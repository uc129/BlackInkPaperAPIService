using Dapper;
using Application.DTOs.Products;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Mappers;
using Infrastructure.Persistence;
using System.Data;

namespace Infrastructure.Repositories;

public class ProductRepository(IDapperContext dapperContext) : IProductRepository
{
    public async Task<ProductAggregate?> GetById(int id)
        => await GetSingleByColumn("Id", id);

    public async Task<ProductAggregate?> GetBySlug(string slug)
        => await GetSingleByColumn("Slug", slug);

    public async Task<IEnumerable<ProductAggregate>> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Products
            ORDER BY Id DESC;
            """;

        using var connection = dapperContext.CreateConnection();
        var rows = (await connection.QueryAsync<ProductRepositoryMapper.ProductRow>(sql)).ToList();
        return rows.Select(row => ProductRepositoryMapper.ToAggregate(row, null, [], [], []));
    }

    public async Task<(IEnumerable<ProductAggregate> Items, int TotalCount)> Search(
        string? query,
        int? artistId,
        int? categoryId,
        int? subCategoryId,
        int? tagId,
        bool? isAvailable,
        bool? isFeatured,
        int page,
        int pageSize)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Products p
            WHERE
                (@Query IS NULL OR p.Name LIKE @LikeQuery OR p.ProductId LIKE @LikeQuery OR p.Slug LIKE @LikeQuery OR p.NameCode LIKE @LikeQuery)
                AND (@ArtistId IS NULL OR p.ArtistId = @ArtistId)
                AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                AND (@SubCategoryId IS NULL OR p.SubCategoryId = @SubCategoryId)
                AND (@IsAvailable IS NULL OR p.IsAvailable = @IsAvailable)
                AND (@IsFeatured IS NULL OR p.IsFeatured = @IsFeatured)
                AND (
                    @TagId IS NULL OR EXISTS (
                        SELECT 1
                        FROM Map_ProductTags m
                        WHERE m.ProductId = p.Id AND m.ProductTagId = @TagId
                    )
                );

            SELECT p.*
            FROM Products p
            WHERE
                (@Query IS NULL OR p.Name LIKE @LikeQuery OR p.ProductId LIKE @LikeQuery OR p.Slug LIKE @LikeQuery OR p.NameCode LIKE @LikeQuery)
                AND (@ArtistId IS NULL OR p.ArtistId = @ArtistId)
                AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                AND (@SubCategoryId IS NULL OR p.SubCategoryId = @SubCategoryId)
                AND (@IsAvailable IS NULL OR p.IsAvailable = @IsAvailable)
                AND (@IsFeatured IS NULL OR p.IsFeatured = @IsFeatured)
                AND (
                    @TagId IS NULL OR EXISTS (
                        SELECT 1
                        FROM Map_ProductTags m
                        WHERE m.ProductId = p.Id AND m.ProductTagId = @TagId
                    )
                )
            ORDER BY p.UpdatedAt DESC, p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Query = normalizedQuery,
            LikeQuery = normalizedQuery is null ? null : $"%{normalizedQuery}%",
            ArtistId = artistId,
            CategoryId = categoryId,
            SubCategoryId = subCategoryId,
            TagId = tagId,
            IsAvailable = isAvailable,
            IsFeatured = isFeatured,
            Offset = (normalizedPage - 1) * normalizedPageSize,
            PageSize = normalizedPageSize
        });

        var totalCount = await multi.ReadSingleAsync<int>();
        var rows = (await multi.ReadAsync<ProductRepositoryMapper.ProductRow>()).ToList();
        var items = rows.Select(row => ProductRepositoryMapper.ToAggregate(row, null, [], [], []));

        return (items, totalCount);
    }

    public async Task<int> Add(ProductAggregate product)
    {
        const string insertProductSql = """
            INSERT INTO Products
            (
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
            (
                @ArtistId,
                @ProductId,
                @Name,
                @Slug,
                @NameCode,
                @PrintName,
                @Description,
                @ShortDescription,
                @BasePrice,
                @FinalPrice,
                @CurrencyCode,
                @CategoryId,
                @SubCategoryId,
                @IsFeatured,
                @IsAvailable,
                @CoverImageUrl,
                @HeaderImageUrl,
                @AverageRating,
                @ReviewCount,
                @StockQuantity,
                @CreatedAt,
                @CreatedBy,
                @UpdatedAt,
                @UpdatedBy,
                @ArtSpecId,
                @IsUsingStandardVariants
            );

            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            product.ArtSpecId = await UpsertArtSpecifications(connection, transaction, product.ArtSpecId, product.ArtSpecs);

            var productId = await connection.ExecuteScalarAsync<int>(
                insertProductSql,
                ProductRepositoryMapper.ToProductParameters(product),
                transaction);

            product.Id = productId;

            await ReplaceProductImages(connection, transaction, product.Id, product.Images);
            await ReplaceProductTags(connection, transaction, product.Id, product.Tags.Select(tag => tag.Id).ToList());
            await ReplaceProductVariants(connection, transaction, product.Id, product.Variants);

            transaction.Commit();
            return productId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task Update(ProductAggregate product)
    {
        const string updateProductSql = """
            UPDATE Products
            SET
                ProductId = @ProductId,
                Name = @Name,
                Slug = @Slug,
                NameCode = @NameCode,
                PrintName = @PrintName,
                Description = @Description,
                ShortDescription = @ShortDescription,
                BasePrice = @BasePrice,
                FinalPrice = @FinalPrice,
                CurrencyCode = @CurrencyCode,
                CategoryId = @CategoryId,
                SubCategoryId = @SubCategoryId,
                IsFeatured = @IsFeatured,
                IsAvailable = @IsAvailable,
                CoverImageUrl = @CoverImageUrl,
                HeaderImageUrl = @HeaderImageUrl,
                StockQuantity = @StockQuantity,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy,
                ArtSpecId = @ArtSpecId,
                IsUsingStandardVariants = @IsUsingStandardVariants
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            product.ArtSpecId = await UpsertArtSpecifications(connection, transaction, product.ArtSpecId, product.ArtSpecs);

            await connection.ExecuteAsync(
                updateProductSql,
                ProductRepositoryMapper.ToProductParameters(product),
                transaction);

            await ReplaceProductImages(connection, transaction, product.Id, product.Images);
            await ReplaceProductTags(connection, transaction, product.Id, product.Tags.Select(tag => tag.Id).ToList());
            await ReplaceProductVariants(connection, transaction, product.Id, product.Variants);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateFlags(int id, bool? isAvailable, bool? isFeatured, DateTime updatedAt, string updatedBy)
    {
        const string sql = """
            UPDATE Products
            SET
                IsAvailable = COALESCE(@IsAvailable, IsAvailable),
                IsFeatured = COALESCE(@IsFeatured, IsFeatured),
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            IsAvailable = isAvailable,
            IsFeatured = isFeatured,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        });
    }

    public async Task Delete(ProductAggregate product)
    {
        const string sql = """
            DELETE FROM ProductVariantOptions
            WHERE ProductVariantId IN (SELECT Id FROM ProductVariants WHERE ProductId = @Id);

            DELETE FROM ProductVariants
            WHERE ProductId = @Id;

            DELETE FROM ProductImages
            WHERE ProductId = @Id;

            DELETE FROM Map_ProductTags
            WHERE ProductId = @Id;

            DELETE FROM Products
            WHERE Id = @Id;

            DELETE FROM ArtSpecifications
            WHERE Id = @ArtSpecId;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { product.Id, product.ArtSpecId });
    }

    public async Task<bool> ExistsBySlug(string slug, int? excludedProductId = null)
        => await ExistsInProducts("Slug", slug, excludedProductId);

    public async Task<bool> ExistsByProductId(string productId, int? excludedProductId = null)
        => await ExistsInProducts("ProductId", productId, excludedProductId);

    public async Task<bool> ExistsByNameCode(string nameCode, int? excludedProductId = null)
        => await ExistsInProducts("NameCode", nameCode, excludedProductId);

    public async Task<bool> ArtistExists(int artistId)
        => await ExistsById("ArtistProfiles", artistId);

    public async Task<bool> CategoryExists(int categoryId)
        => await ExistsById("ProductCategories", categoryId);

    public async Task<bool> SubCategoryExists(int subCategoryId)
        => await ExistsById("ProductSubCategories", subCategoryId);

    public async Task<bool> ArtSpecificationExists(int artSpecId)
        => await ExistsById("ArtSpecifications", artSpecId);

    public async Task<IReadOnlyList<ArtistLookupDto>> GetArtists()
    {
        const string sql = """
            SELECT
                Id,
                DisplayName,
                ProfileImageUrl,
                IsVerified
            FROM ArtistProfiles
            ORDER BY DisplayName, Id;
            """;

        using var connection = dapperContext.CreateConnection();
        var items = await connection.QueryAsync<ArtistLookupDto>(sql);
        return items.ToList();
    }

    public async Task<IReadOnlyList<ProductCategoryLookupDto>> GetCategories()
    {
        const string sql = """
            SELECT
                Id,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured
            FROM ProductCategories
            ORDER BY PrintName, Name, Id;
            """;

        using var connection = dapperContext.CreateConnection();
        var items = await connection.QueryAsync<ProductCategoryLookupDto>(sql);
        return items.ToList();
    }

    public async Task<IReadOnlyList<ProductSubCategoryLookupDto>> GetSubCategories(int? categoryId = null)
    {
        const string sql = """
            SELECT
                Id,
                CategoryId,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured
            FROM ProductSubCategories
            WHERE @CategoryId IS NULL OR CategoryId = @CategoryId
            ORDER BY PrintName, Name, Id;
            """;

        using var connection = dapperContext.CreateConnection();
        var items = await connection.QueryAsync<ProductSubCategoryLookupDto>(sql, new { CategoryId = categoryId });
        return items.ToList();
    }

    public async Task<IReadOnlyList<ProductTagDto>> GetTags()
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Slug,
                Color
            FROM ProductTags
            ORDER BY Name, Id;
            """;

        using var connection = dapperContext.CreateConnection();
        var items = await connection.QueryAsync<ProductTagDto>(sql);
        return items.ToList();
    }

    public async Task<ProductCategoryLookupDto> CreateCategory(CreateProductCategoryRequest request)
    {
        const string sql = """
            INSERT INTO ProductCategories
            (
                NameCode,
                Name,
                PrintName,
                Description,
                IsActive,
                IsFeatured,
                Slug,
                CoverImageUrl
            )
            VALUES
            (
                @NameCode,
                @Name,
                @PrintName,
                @Description,
                @IsActive,
                @IsFeatured,
                @Slug,
                @CoverImageUrl
            );

            SELECT
                Id,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured
            FROM ProductCategories
            WHERE Id = CAST(SCOPE_IDENTITY() AS int);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleAsync<ProductCategoryLookupDto>(sql, request);
    }

    public async Task<ProductSubCategoryLookupDto> CreateSubCategory(CreateProductSubCategoryRequest request)
    {
        const string sql = """
            INSERT INTO ProductSubCategories
            (
                CategoryId,
                NameCode,
                Name,
                PrintName,
                Description,
                IsActive,
                IsFeatured,
                Slug,
                CoverImageUrl
            )
            VALUES
            (
                @CategoryId,
                @NameCode,
                @Name,
                @PrintName,
                @Description,
                @IsActive,
                @IsFeatured,
                @Slug,
                @CoverImageUrl
            );

            SELECT
                Id,
                CategoryId,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured
            FROM ProductSubCategories
            WHERE Id = CAST(SCOPE_IDENTITY() AS int);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleAsync<ProductSubCategoryLookupDto>(sql, request);
    }

    public async Task<bool> CategorySlugExists(string slug)
        => await ExistsBySlug("ProductCategories", slug);

    public async Task<bool> SubCategorySlugExists(string slug)
        => await ExistsBySlug("ProductSubCategories", slug);

    private async Task<ProductAggregate?> GetSingleByColumn<TValue>(string columnName, TValue value)
    {
        var sql = $"""
            SELECT *
            FROM Products
            WHERE {columnName} = @Value;

            SELECT TOP 1 specs.*
            FROM ArtSpecifications specs
            INNER JOIN Products p ON p.ArtSpecId = specs.Id
            WHERE p.{columnName} = @Value;

            SELECT *
            FROM ProductImages
            WHERE ProductId = (SELECT TOP 1 Id FROM Products WHERE {columnName} = @Value)
            ORDER BY DisplayOrder, Id;

            SELECT tags.*
            FROM ProductTags tags
            INNER JOIN Map_ProductTags map ON map.ProductTagId = tags.Id
            WHERE map.ProductId = (SELECT TOP 1 Id FROM Products WHERE {columnName} = @Value)
            ORDER BY tags.Id;

            SELECT *
            FROM ProductVariants
            WHERE ProductId = (SELECT TOP 1 Id FROM Products WHERE {columnName} = @Value)
            ORDER BY Id;

            SELECT *
            FROM ProductVariantOptions
            WHERE ProductVariantId IN (
                SELECT Id
                FROM ProductVariants
                WHERE ProductId = (SELECT TOP 1 Id FROM Products WHERE {columnName} = @Value)
            )
            ORDER BY ProductVariantId, Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new { Value = value });

        var productRow = await multi.ReadSingleOrDefaultAsync<ProductRepositoryMapper.ProductRow>();
        if (productRow is null)
        {
            return null;
        }

        var artSpecifications = await multi.ReadSingleOrDefaultAsync<ProductRepositoryMapper.ArtSpecificationsRow>();
        var images = (await multi.ReadAsync<ProductImage>()).ToList();
        var tags = (await multi.ReadAsync<ProductTag>()).ToList();
        var variants = (await multi.ReadAsync<ProductVariantAggregate>()).ToList();
        var options = (await multi.ReadAsync<ProductVariantOption>()).ToList();

        foreach (var variant in variants)
        {
            variant.Options = options.Where(option => option.ProductVariantId == variant.Id).ToList();
        }

        return ProductRepositoryMapper.ToAggregate(productRow, artSpecifications, images, tags, variants);
    }

    private static async Task<int> UpsertArtSpecifications(
        IDbConnection connection,
        IDbTransaction transaction,
        int existingArtSpecId,
        ArtSpecifications artSpecifications)
    {
        const string insertSql = """
            INSERT INTO ArtSpecifications
            (
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
            (
                @Width,
                @Height,
                @Unit,
                @WeightGrams,
                @IsFramed,
                @Material,
                @FileFormat,
                @ResolutionDpi,
                @PixelDimensions
            );

            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        const string updateSql = """
            UPDATE ArtSpecifications
            SET
                Width = @Width,
                Height = @Height,
                Unit = @Unit,
                WeightGrams = @WeightGrams,
                IsFramed = @IsFramed,
                Material = @Material,
                FileFormat = @FileFormat,
                ResolutionDpi = @ResolutionDpi,
                PixelDimensions = @PixelDimensions
            WHERE Id = @Id;
            """;

        if (existingArtSpecId > 0)
        {
            var rowsAffected = await connection.ExecuteAsync(
                updateSql,
                ProductRepositoryMapper.ToArtSpecificationsParameters(existingArtSpecId, artSpecifications),
                transaction);

            if (rowsAffected > 0)
            {
                return existingArtSpecId;
            }
        }

        return await connection.ExecuteScalarAsync<int>(
            insertSql,
            ProductRepositoryMapper.ToArtSpecificationsParameters(null, artSpecifications),
            transaction);
    }

    private async Task<bool> ExistsInProducts(string columnName, string value, int? excludedProductId)
    {
        var sql = $"""
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM Products
                WHERE {columnName} = @Value
                  AND (@ExcludedProductId IS NULL OR Id <> @ExcludedProductId)
            ) THEN 1 ELSE 0 END AS bit);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Value = value, ExcludedProductId = excludedProductId });
    }

    private async Task<bool> ExistsById(string tableName, int id)
    {
        var sql = $"""
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE Id = @Id
            ) THEN 1 ELSE 0 END AS bit);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    private async Task<bool> ExistsBySlug(string tableName, string slug)
    {
        var sql = $"""
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE Slug = @Slug
            ) THEN 1 ELSE 0 END AS bit);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Slug = slug });
    }

    private static async Task ReplaceProductImages(
        IDbConnection connection,
        IDbTransaction transaction,
        int productId,
        IEnumerable<ProductImage> images)
    {
        const string deleteSql = """
            DELETE FROM ProductImages
            WHERE ProductId = @ProductId;
            """;

        const string insertSql = """
            INSERT INTO ProductImages
            (
                ProductId,
                AltText,
                IsPrimary,
                DisplayOrder,
                PublicId,
                BaseUrl,
                AspectRatio,
                Width,
                Height,
                PlaceholderUrl
            )
            VALUES
            (
                @ProductId,
                @AltText,
                @IsPrimary,
                @DisplayOrder,
                @PublicId,
                @BaseUrl,
                @AspectRatio,
                @Width,
                @Height,
                @PlaceholderUrl
            );
            """;

        await connection.ExecuteAsync(deleteSql, new { ProductId = productId }, transaction);

        foreach (var image in images)
        {
            image.ProductId = productId;
            await connection.ExecuteAsync(insertSql, image, transaction);
        }
    }

    private static async Task ReplaceProductTags(
        IDbConnection connection,
        IDbTransaction transaction,
        int productId,
        IReadOnlyCollection<int> tagIds)
    {
        const string deleteSql = """
            DELETE FROM Map_ProductTags
            WHERE ProductId = @ProductId;
            """;

        const string insertSql = """
            INSERT INTO Map_ProductTags (ProductId, ProductTagId)
            VALUES (@ProductId, @ProductTagId);
            """;

        await connection.ExecuteAsync(deleteSql, new { ProductId = productId }, transaction);

        foreach (var tagId in tagIds.Distinct())
        {
            await connection.ExecuteAsync(insertSql, new { ProductId = productId, ProductTagId = tagId }, transaction);
        }
    }

    private static async Task ReplaceProductVariants(
        IDbConnection connection,
        IDbTransaction transaction,
        int productId,
        IEnumerable<ProductVariantAggregate> variants)
    {
        const string deleteOptionsSql = """
            DELETE FROM ProductVariantOptions
            WHERE ProductVariantId IN (SELECT Id FROM ProductVariants WHERE ProductId = @ProductId);
            """;

        const string deleteVariantsSql = """
            DELETE FROM ProductVariants
            WHERE ProductId = @ProductId;
            """;

        const string insertVariantSql = """
            INSERT INTO ProductVariants (ProductId, Label)
            VALUES (@ProductId, @Label);

            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        const string insertOptionSql = """
            INSERT INTO ProductVariantOptions
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
            (
                @ProductVariantId,
                @Value,
                @PriceModifier,
                @AbsolutePrice,
                @StockQuantity,
                @FulfillmentType,
                @Sku,
                @WeightGrams
            );
            """;

        await connection.ExecuteAsync(deleteOptionsSql, new { ProductId = productId }, transaction);
        await connection.ExecuteAsync(deleteVariantsSql, new { ProductId = productId }, transaction);

        foreach (var variant in variants)
        {
            variant.ProductId = productId;
            var variantId = await connection.ExecuteScalarAsync<int>(
                insertVariantSql,
                new { ProductId = productId, variant.Label },
                transaction);

            foreach (var option in variant.Options)
            {
                option.ProductVariantId = variantId;
                await connection.ExecuteAsync(insertOptionSql, new
                {
                    option.ProductVariantId,
                    option.Value,
                    option.PriceModifier,
                    option.AbsolutePrice,
                    option.StockQuantity,
                    FulfillmentType = (int)option.FulfillmentType,
                    option.Sku,
                    option.WeightGrams
                }, transaction);
            }
        }
    }

}
