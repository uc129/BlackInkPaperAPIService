using BlackInkPaperAPIService.Tests.Products.Fakes;
using Infrastructure.Services;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Products;

public class ProductApplicationServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenProductDoesNotExist()
    {
        var repository = new FakeProductRepository();
        var service = new ProductApplicationService(repository);

        var response = await service.GetByIdAsync(42);

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Contains("42", response.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenSlugAlreadyExists()
    {
        var repository = new FakeProductRepository
        {
            ExistsBySlugHandler = (_, _) => Task.FromResult(true)
        };
        var service = new ProductApplicationService(repository);

        var response = await service.CreateAsync(ProductTestData.CreateRequest());

        Assert.False(response.Success);
        Assert.Contains("Slug", response.Message);
        Assert.Null(repository.AddedProduct);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedProduct_WhenRequestIsValid()
    {
        var created = ProductTestData.Aggregate(id: 101);
        var repository = new FakeProductRepository
        {
            AddHandler = product =>
            {
                product.Id = 101;
                return Task.FromResult(101);
            },
            GetByIdHandler = id => Task.FromResult(id == 101 ? created : null)
        };
        var service = new ProductApplicationService(repository);

        var response = await service.CreateAsync(ProductTestData.CreateRequest());

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(101, response.Data!.Id);
        Assert.Equal("ART-001", repository.AddedProduct!.ProductId);
        Assert.Equal("sunset-art", repository.AddedProduct.Slug);
        Assert.Single(repository.AddedProduct.Images);
        Assert.Single(repository.AddedProduct.Variants);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenOriginalHasStockOtherThanOne()
    {
        var repository = new FakeProductRepository();
        var service = new ProductApplicationService(repository);

        var response = await service.CreateAsync(ProductTestData.OriginalCreateRequest(stockQuantity: 5));

        Assert.False(response.Success);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("one available piece", response.Message);
        Assert.Null(repository.AddedProduct);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenOriginalUsesStandardVariants()
    {
        var repository = new FakeProductRepository();
        var service = new ProductApplicationService(repository);

        var response = await service.CreateAsync(
            ProductTestData.OriginalCreateRequest(stockQuantity: 1, isUsingStandardVariants: true));

        Assert.False(response.Success);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("standard variants", response.Message);
        Assert.Null(repository.AddedProduct);
    }

    [Fact]
    public async Task CreateAsync_CreatesOriginal_WhenSinglePieceAndNoStandardVariants()
    {
        var created = ProductTestData.Aggregate(id: 202);
        var repository = new FakeProductRepository
        {
            AddHandler = product =>
            {
                product.Id = 202;
                return Task.FromResult(202);
            },
            GetByIdHandler = id => Task.FromResult(id == 202 ? created : null)
        };
        var service = new ProductApplicationService(repository);

        var response = await service.CreateAsync(ProductTestData.OriginalCreateRequest(stockQuantity: 1));

        Assert.True(response.Success);
        Assert.NotNull(repository.AddedProduct);
        Assert.True(repository.AddedProduct!.ArtSpecs.IsOriginal);
        Assert.Equal(1, repository.AddedProduct.StockQuantity);
    }

    [Fact]
    public async Task SearchAsync_NormalizesPaging_AndReturnsSummaries()
    {
        var product = ProductTestData.Aggregate(id: 10);
        var repository = new FakeProductRepository
        {
            SearchHandler = (query, artistId, categoryId, subCategoryId, tagId, isAvailable, isFeatured, page, pageSize) =>
            {
                Assert.Equal(1, page);
                Assert.Equal(100, pageSize);
                Assert.Equal("sunset", query);
                return Task.FromResult<(IEnumerable<Domain.Aggregates.Ecommerce.ProductAggregate>, int)>(([product], 1));
            }
        };
        var service = new ProductApplicationService(repository);

        var response = await service.SearchAsync(new Application.DTOs.Products.ProductSearchRequest(
            Query: "sunset",
            ArtistId: null,
            CategoryId: null,
            SubCategoryId: null,
            TagId: null,
            IsAvailable: true,
            IsFeatured: null,
            Page: 0,
            PageSize: 500));

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!.Items);
        Assert.Equal(1, response.Data.Page);
        Assert.Equal(100, response.Data.PageSize);
        Assert.Equal("Sunset Print", response.Data.Items[0].Name);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyPage_WhenCategorySlugMatchesNothing()
    {
        var repository = new FakeProductRepository();
        var service = new ProductApplicationService(repository);

        var response = await service.SearchAsync(new Application.DTOs.Products.ProductSearchRequest(
            Query: null,
            ArtistId: null,
            CategoryId: null,
            SubCategoryId: null,
            TagId: null,
            IsAvailable: true,
            IsFeatured: null,
            CategorySlug: "originals"));

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data!.Items);
        Assert.Equal(0, response.Data.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_MapsRequestOntoExistingAggregate_AndPersists()
    {
        var existing = ProductTestData.Aggregate(id: 55);
        var updated = ProductTestData.Aggregate(id: 55, productId: "ART-001-UPDATED", slug: "sunset-art-updated");
        updated.Name = "Sunset Print Updated";
        updated.NameCode = "SUNSET-UPDATED";
        var repository = new FakeProductRepository
        {
            GetByIdHandler = id => Task.FromResult(id == 55 ? existing : null),
            UpdateHandler = _ => Task.CompletedTask
        };
        var service = new ProductApplicationService(repository);

        var response = await service.UpdateAsync(55, ProductTestData.UpdateRequest());

        Assert.True(response.Success);
        Assert.NotNull(repository.UpdatedProduct);
        Assert.Equal("ART-001-UPDATED", repository.UpdatedProduct!.ProductId);
        Assert.Equal("sunset-art-updated", repository.UpdatedProduct.Slug);
        Assert.True(repository.UpdatedProduct.IsUsingStandardVariants);
        Assert.Single(repository.UpdatedProduct.Tags);
    }

    [Fact]
    public async Task DeleteAsync_DeletesExistingProduct()
    {
        var existing = ProductTestData.Aggregate(id: 88);
        var repository = new FakeProductRepository
        {
            GetByIdHandler = id => Task.FromResult(id == 88 ? existing : null)
        };
        var service = new ProductApplicationService(repository);

        var response = await service.DeleteAsync(88);

        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.Equal(existing, repository.DeletedProduct);
    }
}
