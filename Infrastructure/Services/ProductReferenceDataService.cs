using Application.DTOs.Products;
using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;

namespace Infrastructure.Services;

public class ProductReferenceDataService(IProductRepository productRepository) : IProductReferenceDataService
{
    public async Task<ServiceResponse<IReadOnlyList<ArtistLookupDto>>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var artists = await productRepository.GetArtists();
            return ServiceResponse<IReadOnlyList<ArtistLookupDto>>.Ok(artists);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<ArtistLookupDto>>.Fail("Unable to load artists.", ex.ToString(), 500, "artist_lookup_failed");
        }
    }

    public async Task<ServiceResponse<IReadOnlyList<ProductCategoryLookupDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await productRepository.GetCategories();
            return ServiceResponse<IReadOnlyList<ProductCategoryLookupDto>>.Ok(categories);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<ProductCategoryLookupDto>>.Fail("Unable to load categories.", ex.ToString(), 500, "category_lookup_failed");
        }
    }

    public async Task<ServiceResponse<IReadOnlyList<ProductSubCategoryLookupDto>>> GetSubCategoriesAsync(int? categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subCategories = await productRepository.GetSubCategories(categoryId);
            return ServiceResponse<IReadOnlyList<ProductSubCategoryLookupDto>>.Ok(subCategories);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<ProductSubCategoryLookupDto>>.Fail("Unable to load subcategories.", ex.ToString(), 500, "subcategory_lookup_failed");
        }
    }

    public async Task<ServiceResponse<IReadOnlyList<ProductTagDto>>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await productRepository.GetTags();
            return ServiceResponse<IReadOnlyList<ProductTagDto>>.Ok(tags);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<ProductTagDto>>.Fail("Unable to load tags.", ex.ToString(), 500, "tag_lookup_failed");
        }
    }

    public async Task<ServiceResponse<ProductCategoryLookupDto>> CreateCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationError = ValidateLookupPayload(request.NameCode, request.Name, request.PrintName, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductCategoryLookupDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.CategorySlugExists(request.Slug.Trim()))
            {
                return ServiceResponse<ProductCategoryLookupDto>.Fail($"Category slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var category = await productRepository.CreateCategory(request with
            {
                NameCode = request.NameCode.Trim(),
                Name = request.Name.Trim(),
                PrintName = request.PrintName.Trim(),
                Slug = request.Slug.Trim()
            });

            return ServiceResponse<ProductCategoryLookupDto>.Ok(category, "Category created successfully.", 201);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductCategoryLookupDto>.Fail("Unable to create category.", ex.ToString(), 500, "category_create_failed");
        }
    }

    public async Task<ServiceResponse<ProductSubCategoryLookupDto>> CreateSubCategoryAsync(CreateProductSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.CategoryExists(request.CategoryId))
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Category with id {request.CategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
            }

            var validationError = ValidateLookupPayload(request.NameCode, request.Name, request.PrintName, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.SubCategorySlugExists(request.Slug.Trim()))
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Subcategory slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var subCategory = await productRepository.CreateSubCategory(request with
            {
                NameCode = request.NameCode.Trim(),
                Name = request.Name.Trim(),
                PrintName = request.PrintName.Trim(),
                Slug = request.Slug.Trim()
            });

            return ServiceResponse<ProductSubCategoryLookupDto>.Ok(subCategory, "Subcategory created successfully.", 201);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductSubCategoryLookupDto>.Fail("Unable to create subcategory.", ex.ToString(), 500, "subcategory_create_failed");
        }
    }

    private static string? ValidateLookupPayload(string nameCode, string name, string printName, string slug)
    {
        if (string.IsNullOrWhiteSpace(nameCode)) return "NameCode is required.";
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(printName)) return "PrintName is required.";
        if (string.IsNullOrWhiteSpace(slug)) return "Slug is required.";
        return null;
    }
}
