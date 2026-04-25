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
                        WHERE m.ProductId = p.Id AND m.TagId = @TagId
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
                        WHERE m.ProductId = p.Id AND m.TagId = @TagId
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
                @IsUsingStandardVariants
            ),
            RETURNING (Id);
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var productId = await connection.ExecuteScalarAsync<int>(
                insertProductSql,
                ProductRepositoryMapper.ToProductParameters(product),
                transaction);

            product.Id = productId;

            await UpsertArtSpecifications(connection, transaction, product.Id, product.ArtSpecs);

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
                IsUsingStandardVariants = @IsUsingStandardVariants
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                updateProductSql,
                ProductRepositoryMapper.ToProductParameters(product),
                transaction);

            await UpsertArtSpecifications(connection, transaction, product.Id, product.ArtSpecs);

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

            DELETE FROM ArtSpecifications
            WHERE ProductId = @Id;

            DELETE FROM Products
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { product.Id });
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
            ),
            RETURNING(
                Id,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured);
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
            ),
            RETURNING(
                Id,
                CategoryId,
                NameCode,
                Name,
                PrintName,
                Slug,
                Description,
                CoverImageUrl,
                IsActive,
                IsFeatured);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleAsync<ProductSubCategoryLookupDto>(sql, request);
    }

    public async Task<ProductCategoryLookupDto?> UpdateCategory(int id, UpdateProductCategoryRequest request)
    {
        const string sql = """
            UPDATE ProductCategories
            SET
                NameCode = @NameCode,
                Name = @Name,
                PrintName = @PrintName,
                Description = @Description,
                IsActive = @IsActive,
                IsFeatured = @IsFeatured,
                Slug = @Slug,
                CoverImageUrl = @CoverImageUrl
            WHERE Id = @Id;

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
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Id = id,
            request.NameCode,
            request.Name,
            request.PrintName,
            request.Description,
            request.IsActive,
            request.IsFeatured,
            request.Slug,
            request.CoverImageUrl
        });

        return await multi.ReadSingleOrDefaultAsync<ProductCategoryLookupDto>();
    }

    public async Task<ProductSubCategoryLookupDto?> UpdateSubCategory(int id, UpdateProductSubCategoryRequest request)
    {
        const string sql = """
            UPDATE ProductSubCategories
            SET
                CategoryId = @CategoryId,
                NameCode = @NameCode,
                Name = @Name,
                PrintName = @PrintName,
                Description = @Description,
                IsActive = @IsActive,
                IsFeatured = @IsFeatured,
                Slug = @Slug,
                CoverImageUrl = @CoverImageUrl
            WHERE Id = @Id;

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
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Id = id,
            request.CategoryId,
            request.NameCode,
            request.Name,
            request.PrintName,
            request.Description,
            request.IsActive,
            request.IsFeatured,
            request.Slug,
            request.CoverImageUrl
        });

        return await multi.ReadSingleOrDefaultAsync<ProductSubCategoryLookupDto>();
    }

    public async Task<ProductTagDto> CreateTag(CreateProductTagRequest request)
    {
        const string sql = """
            INSERT INTO ProductTags
            (
                Name,
                Slug,
                Color,
                CreatedAt
            )
            VALUES
            (
                @Name,
                @Slug,
                @Color,
                CURRENT_TIMESTAMP
            ),
            RETURNING(
                Id,
                Name,
                Slug,
                Color);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleAsync<ProductTagDto>(sql, request);
    }

    public async Task<ProductTagDto?> UpdateTag(int id, UpdateProductTagRequest request)
    {
        const string sql = """
            UPDATE ProductTags
            SET
                Name = @Name,
                Slug = @Slug,
                Color = @Color
            WHERE Id = @Id;

            SELECT
                Id,
                Name,
                Slug,
                Color
            FROM ProductTags
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Id = id,
            request.Name,
            request.Slug,
            request.Color
        });

        return await multi.ReadSingleOrDefaultAsync<ProductTagDto>();
    }

    public async Task DeleteCategory(int id)
    {
        const string sql = """
            DELETE FROM ProductCategories
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteSubCategory(int id)
    {
        const string sql = """
            DELETE FROM ProductSubCategories
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteTag(int id)
    {
        const string sql = """
            DELETE FROM ProductTags
            WHERE Id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> CategorySlugExists(string slug)
        => await ExistsBySlug("ProductCategories", slug);

    public async Task<bool> CategorySlugExists(string slug, int excludedCategoryId)
        => await ExistsBySlug("ProductCategories", slug, excludedCategoryId);

    public async Task<bool> SubCategorySlugExists(string slug)
        => await ExistsBySlug("ProductSubCategories", slug);

    public async Task<bool> SubCategorySlugExists(string slug, int excludedSubCategoryId)
        => await ExistsBySlug("ProductSubCategories", slug, excludedSubCategoryId);

    public async Task<bool> TagSlugExists(string slug)
        => await ExistsBySlug("ProductTags", slug);

    public async Task<bool> TagSlugExists(string slug, int excludedTagId)
        => await ExistsBySlug("ProductTags", slug, excludedTagId);

    public async Task<bool> TagExists(int tagId)
        => await ExistsById("ProductTags", tagId);

    public async Task<bool> IsCategoryInUse(int categoryId)
        => await ExistsByForeignKey("Products", "CategoryId", categoryId);

    public async Task<bool> IsSubCategoryInUse(int subCategoryId)
        => await ExistsByForeignKey("Products", "SubCategoryId", subCategoryId);

    public async Task<bool> IsTagInUse(int tagId)
        => await ExistsByForeignKey("Map_ProductTags", "TagId", tagId);

    private async Task<ProductAggregate?> GetSingleByColumn<TValue>(string columnName, TValue value)
    {
        var sql = $"""
            SELECT *
            FROM Products
            WHERE {columnName} = @Value;

            SELECT specs.*
            FROM ArtSpecifications specs
            INNER JOIN Products p ON p.Id = specs.ProductId
            WHERE p.{columnName} = @Value
            LIMIT 1;

            SELECT *
            FROM ProductImages
            WHERE ProductId = (SELECT Id FROM Products WHERE {columnName} = @Value LIMIT 1)
            ORDER BY DisplayOrder, Id;

            SELECT tags.*
            FROM ProductTags tags
            INNER JOIN Map_ProductTags map ON map.TagId = tags.Id
            WHERE map.ProductId = (SELECT Id FROM Products WHERE {columnName} = @Value LIMIT 1)
            ORDER BY tags.Id;

            SELECT *
            FROM ProductVariants
            WHERE ProductId = (SELECT Id FROM Products WHERE {columnName} = @Value LIMIT 1)
            ORDER BY Id;

            SELECT *
            FROM ProductVariantOptions
            WHERE ProductVariantId IN (
                SELECT Id
                FROM ProductVariants
                WHERE ProductId = (SELECT Id FROM Products WHERE {columnName} = @Value LIMIT 1)
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

    private static async Task UpsertArtSpecifications(
        IDbConnection connection,
        IDbTransaction transaction,
        int productId,
        ArtSpecifications artSpecifications)
    {
        const string insertSql = """
            INSERT INTO ArtSpecifications
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
            (
                @ProductId,
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
            WHERE ProductId = @ProductId;
            """;

        var rowsAffected = await connection.ExecuteAsync(
            updateSql,
            ProductRepositoryMapper.ToArtSpecificationsParameters(productId, artSpecifications),
            transaction);

        if (rowsAffected == 0)
        {
            await connection.ExecuteAsync(
                insertSql,
                ProductRepositoryMapper.ToArtSpecificationsParameters(productId, artSpecifications),
                transaction);
        }
    }

    private async Task<bool> ExistsInProducts(string columnName, string value, int? excludedProductId)
    {
        var sql = $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM Products
                WHERE {columnName} = @Value
                  AND (@ExcludedProductId IS NULL OR Id <> @ExcludedProductId)
            );
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Value = value, ExcludedProductId = excludedProductId });
    }

    private async Task<bool> ExistsById(string tableName, int id)
    {
        var sql = $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE Id = @Id
            );
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    private async Task<bool> ExistsBySlug(string tableName, string slug)
    {
        var sql = $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE Slug = @Slug
            );
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Slug = slug });
    }

    private async Task<bool> ExistsBySlug(string tableName, string slug, int excludedId)
    {
        var sql = $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE Slug = @Slug
                  AND Id <> @ExcludedId
            );
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Slug = slug, ExcludedId = excludedId });
    }

    private async Task<bool> ExistsByForeignKey(string tableName, string columnName, int value)
    {
        var sql = $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {tableName}
                WHERE {columnName} = @Value
            );
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Value = value });
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
            INSERT INTO Map_ProductTags (ProductId, TagId)
            VALUES (@ProductId, @TagId);
            """;

        await connection.ExecuteAsync(deleteSql, new { ProductId = productId }, transaction);

        foreach (var tagId in tagIds.Distinct())
        {
            await connection.ExecuteAsync(insertSql, new { ProductId = productId, TagId = tagId }, transaction);
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
            INSERT INTO ProductVariants (ProductId, Label, FulfillmentType, Sku, WeightGrams, StockQuantity, AbsolutePrice)
            VALUES (@ProductId, @Label, @FulfillmentType, @Sku, @WeightGrams, @StockQuantity, @AbsolutePrice),
            RETURNING (Id);
            """;

        const string insertOptionSql = """
            INSERT INTO ProductVariantOptions
            (
                ProductVariantId,
                Value,
                PriceModifier,
                AbsolutePrice,
                StockQuantity
            )
            VALUES
            (
                @ProductVariantId,
                @Value,
                @PriceModifier,
                @AbsolutePrice,
                @StockQuantity
            );
            """;

        await connection.ExecuteAsync(deleteOptionsSql, new { ProductId = productId }, transaction);
        await connection.ExecuteAsync(deleteVariantsSql, new { ProductId = productId }, transaction);

        foreach (var variant in variants)
        {
            variant.ProductId = productId;
            var variantId = await connection.ExecuteScalarAsync<int>(
                insertVariantSql,
                new
                {
                    ProductId = productId,
                    variant.Label,
                    FulfillmentType = (int)variant.FulfillmentType,
                    variant.Sku,
                    variant.WeightGrams,
                    variant.StockQuantity,
                    variant.AbsolutePrice
                },
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
                    option.StockQuantity
                }, transaction);
            }
        }
    }

}
