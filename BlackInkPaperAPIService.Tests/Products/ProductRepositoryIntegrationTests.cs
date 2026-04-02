using Dapper;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Products;

public class ProductRepositoryIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Add_GetById_GetBySlug_AndDelete_WorkAgainstSqlServer()
    {
        var connectionString = ProductRepositoryIntegrationTestHelper.GetConnectionStringOrNull();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var template = await ProductRepositoryIntegrationTestHelper.GetTemplateProductOrNull(connectionString);
        if (template is null)
        {
            return;
        }

        var repository = ProductRepositoryIntegrationTestHelper.CreateRepository(connectionString);
        var product = ProductRepositoryIntegrationTestHelper.CreateAggregate(template);
        int createdId = 0;

        try
        {
            createdId = await repository.Add(product);

            var byId = await repository.GetById(createdId);
            var bySlug = await repository.GetBySlug(product.Slug);

            Assert.NotNull(byId);
            Assert.NotNull(bySlug);
            Assert.Equal(product.ProductId, byId!.ProductId);
            Assert.Equal(product.Slug, bySlug!.Slug);
            Assert.Equal(product.NameCode, byId.NameCode);
        }
        finally
        {
            await ProductRepositoryIntegrationTestHelper.CleanupProduct(connectionString, createdId, product.ProductId);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Update_PersistsChangedScalarFields()
    {
        var connectionString = ProductRepositoryIntegrationTestHelper.GetConnectionStringOrNull();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var template = await ProductRepositoryIntegrationTestHelper.GetTemplateProductOrNull(connectionString);
        if (template is null)
        {
            return;
        }

        var repository = ProductRepositoryIntegrationTestHelper.CreateRepository(connectionString);
        var product = ProductRepositoryIntegrationTestHelper.CreateAggregate(template);
        int createdId = 0;

        try
        {
            createdId = await repository.Add(product);
            var saved = await repository.GetById(createdId);
            Assert.NotNull(saved);

            saved!.Name = $"{saved.Name} Updated";
            saved.Slug = $"{saved.Slug}-updated";
            saved.NameCode = $"{saved.NameCode}-UPDATED";
            saved.FinalPrice += 25;
            saved.IsFeatured = !saved.IsFeatured;
            saved.UpdatedAt = DateTime.UtcNow;
            saved.UpdatedBy = "integration-test";

            await repository.Update(saved);

            var updated = await repository.GetById(createdId);

            Assert.NotNull(updated);
            Assert.Equal(saved.Name, updated!.Name);
            Assert.Equal(saved.Slug, updated.Slug);
            Assert.Equal(saved.NameCode, updated.NameCode);
            Assert.Equal(saved.FinalPrice, updated.FinalPrice);
            Assert.Equal("integration-test", updated.UpdatedBy);
        }
        finally
        {
            await ProductRepositoryIntegrationTestHelper.CleanupProduct(connectionString, createdId, product.ProductId);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Search_ReturnsInsertedProduct()
    {
        var connectionString = ProductRepositoryIntegrationTestHelper.GetConnectionStringOrNull();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var template = await ProductRepositoryIntegrationTestHelper.GetTemplateProductOrNull(connectionString);
        if (template is null)
        {
            return;
        }

        var repository = ProductRepositoryIntegrationTestHelper.CreateRepository(connectionString);
        var product = ProductRepositoryIntegrationTestHelper.CreateAggregate(template);
        int createdId = 0;

        try
        {
            createdId = await repository.Add(product);

            var (items, totalCount) = await repository.Search(
                query: product.ProductId,
                artistId: null,
                categoryId: null,
                subCategoryId: null,
                tagId: null,
                isAvailable: null,
                isFeatured: null,
                page: 1,
                pageSize: 10);

            var results = items.ToList();

            Assert.True(totalCount >= 1);
            Assert.Contains(results, item => item.ProductId == product.ProductId);
        }
        finally
        {
            await ProductRepositoryIntegrationTestHelper.CleanupProduct(connectionString, createdId, product.ProductId);
        }
    }
}

internal static class ProductRepositoryIntegrationTestHelper
{
    private const string ConnectionStringEnvVar = "TEST_SQLSERVER_CONNECTION_STRING";

    public static string? GetConnectionStringOrNull()
        => Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

    public static ProductRepository CreateRepository(string connectionString)
        => new(new TestDapperContext(connectionString));

    public static async Task<ProductTemplate?> GetTemplateProductOrNull(string connectionString)
    {
        const string sql = """
            SELECT TOP 1
                Id,
                ArtistId,
                CategoryId,
                SubCategoryId,
                ArtSpecId,
                CurrencyCode
            FROM Products
            ORDER BY Id DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<ProductTemplate>(sql);
    }

    public static ProductAggregate CreateAggregate(ProductTemplate template)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        return new ProductAggregate
        {
            ArtistId = template.ArtistId,
            ProductId = $"TEST-{suffix}",
            Name = $"Integration Test Product {suffix}",
            Slug = $"integration-test-{suffix}",
            NameCode = $"INT-{suffix}",
            PrintName = $"Integration Print {suffix}",
            Description = "Integration test product created by automated tests.",
            ShortDescription = "Integration test",
            BasePrice = 100,
            FinalPrice = 90,
            CurrencyCode = string.IsNullOrWhiteSpace(template.CurrencyCode) ? "INR" : template.CurrencyCode,
            CategoryId = template.CategoryId,
            SubCategoryId = template.SubCategoryId,
            IsFeatured = false,
            IsAvailable = true,
            CoverImageUrl = string.Empty,
            HeaderImageUrl = string.Empty,
            AverageRating = 0,
            ReviewCount = 0,
            StockQuantity = 1,
            CreatedAt = now,
            CreatedBy = "integration-test",
            UpdatedAt = now,
            UpdatedBy = "integration-test",
            ArtSpecId = template.ArtSpecId,
            ArtSpecs = new ArtSpecifications
            {
                PhysicalDimensions = new Dimensions
                {
                    Width = 20,
                    Height = 30,
                    Unit = DimensionUnits.cm
                },
                WeightGrams = 150,
                IsFramed = false,
                Material = "Canvas"
            },
            IsUsingStandardVariants = false,
            Images = [],
            Tags = [],
            Variants = []
        };
    }

    public static async Task CleanupProduct(string connectionString, int createdId, string productId)
    {
        const string sql = """
            DECLARE @TargetId int = @CreatedId;

            IF @TargetId = 0
            BEGIN
                SELECT TOP 1 @TargetId = Id
                FROM Products
                WHERE ProductId = @ProductId;
            END

            IF @TargetId IS NOT NULL
            BEGIN
                DELETE FROM ProductVariantOptions
                WHERE ProductVariantId IN (SELECT Id FROM ProductVariants WHERE ProductId = @TargetId);

                DELETE FROM ProductVariants
                WHERE ProductId = @TargetId;

                DELETE FROM ProductImages
                WHERE ProductId = @TargetId;

                DELETE FROM Map_ProductTags
                WHERE ProductId = @TargetId;

                DELETE FROM Products
                WHERE Id = @TargetId;
            END
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync(sql, new { CreatedId = createdId, ProductId = productId });
    }

    public sealed class ProductTemplate
    {
        public int Id { get; init; }
        public int ArtistId { get; init; }
        public int CategoryId { get; init; }
        public int SubCategoryId { get; init; }
        public int ArtSpecId { get; init; }
        public string CurrencyCode { get; init; } = "INR";
    }

    private sealed class TestDapperContext(string connectionString) : IDapperContext
    {
        public System.Data.IDbConnection CreateConnection(string name = "DefaultConnection")
            => new SqlConnection(connectionString);
    }
}
