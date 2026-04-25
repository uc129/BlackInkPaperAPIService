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

    public async Task<ServiceResponse<ProductCategoryLookupDto>> UpdateCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.CategoryExists(id))
            {
                return ServiceResponse<ProductCategoryLookupDto>.Fail($"Category with id {id} does not exist.", statusCode: 404, errorCode: "category_not_found");
            }

            var validationError = ValidateLookupPayload(request.NameCode, request.Name, request.PrintName, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductCategoryLookupDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.CategorySlugExists(request.Slug.Trim(), id))
            {
                return ServiceResponse<ProductCategoryLookupDto>.Fail($"Category slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var updated = await productRepository.UpdateCategory(id, request with
            {
                NameCode = request.NameCode.Trim(),
                Name = request.Name.Trim(),
                PrintName = request.PrintName.Trim(),
                Slug = request.Slug.Trim()
            });

            return updated is null
                ? ServiceResponse<ProductCategoryLookupDto>.Fail($"Category with id {id} does not exist.", statusCode: 404, errorCode: "category_not_found")
                : ServiceResponse<ProductCategoryLookupDto>.Ok(updated, "Category updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductCategoryLookupDto>.Fail("Unable to update category.", ex.ToString(), 500, "category_update_failed");
        }
    }

    public async Task<ServiceResponse<ProductSubCategoryLookupDto>> UpdateSubCategoryAsync(int id, UpdateProductSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.SubCategoryExists(id))
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Subcategory with id {id} does not exist.", statusCode: 404, errorCode: "subcategory_not_found");
            }

            if (!await productRepository.CategoryExists(request.CategoryId))
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Category with id {request.CategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
            }

            var validationError = ValidateLookupPayload(request.NameCode, request.Name, request.PrintName, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.SubCategorySlugExists(request.Slug.Trim(), id))
            {
                return ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Subcategory slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var updated = await productRepository.UpdateSubCategory(id, request with
            {
                NameCode = request.NameCode.Trim(),
                Name = request.Name.Trim(),
                PrintName = request.PrintName.Trim(),
                Slug = request.Slug.Trim()
            });

            return updated is null
                ? ServiceResponse<ProductSubCategoryLookupDto>.Fail($"Subcategory with id {id} does not exist.", statusCode: 404, errorCode: "subcategory_not_found")
                : ServiceResponse<ProductSubCategoryLookupDto>.Ok(updated, "Subcategory updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductSubCategoryLookupDto>.Fail("Unable to update subcategory.", ex.ToString(), 500, "subcategory_update_failed");
        }
    }

    public async Task<ServiceResponse<ProductTagDto>> CreateTagAsync(CreateProductTagRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationError = ValidateTagPayload(request.Name, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductTagDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.TagSlugExists(request.Slug.Trim()))
            {
                return ServiceResponse<ProductTagDto>.Fail($"Tag slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var created = await productRepository.CreateTag(request with
            {
                Name = request.Name.Trim(),
                Slug = request.Slug.Trim(),
                Color = NormalizeOptional(request.Color)
            });

            return ServiceResponse<ProductTagDto>.Ok(created, "Tag created successfully.", 201);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductTagDto>.Fail("Unable to create tag.", ex.ToString(), 500, "tag_create_failed");
        }
    }

    public async Task<ServiceResponse<ProductTagDto>> UpdateTagAsync(int id, UpdateProductTagRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.TagExists(id))
            {
                return ServiceResponse<ProductTagDto>.Fail($"Tag with id {id} does not exist.", statusCode: 404, errorCode: "tag_not_found");
            }

            var validationError = ValidateTagPayload(request.Name, request.Slug);
            if (validationError is not null)
            {
                return ServiceResponse<ProductTagDto>.Fail(validationError, statusCode: 400, errorCode: "validation_error");
            }

            if (await productRepository.TagSlugExists(request.Slug.Trim(), id))
            {
                return ServiceResponse<ProductTagDto>.Fail($"Tag slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
            }

            var updated = await productRepository.UpdateTag(id, request with
            {
                Name = request.Name.Trim(),
                Slug = request.Slug.Trim(),
                Color = NormalizeOptional(request.Color)
            });

            return updated is null
                ? ServiceResponse<ProductTagDto>.Fail($"Tag with id {id} does not exist.", statusCode: 404, errorCode: "tag_not_found")
                : ServiceResponse<ProductTagDto>.Ok(updated, "Tag updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductTagDto>.Fail("Unable to update tag.", ex.ToString(), 500, "tag_update_failed");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.CategoryExists(id))
            {
                return ServiceResponse<bool>.Fail($"Category with id {id} does not exist.", statusCode: 404, errorCode: "category_not_found");
            }

            if (await productRepository.IsCategoryInUse(id))
            {
                return ServiceResponse<bool>.Fail("Category is linked to one or more products and cannot be deleted.", statusCode: 400, errorCode: "category_in_use");
            }

            await productRepository.DeleteCategory(id);
            return ServiceResponse<bool>.Ok(true, "Category deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to delete category.", ex.ToString(), 500, "category_delete_failed");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSubCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.SubCategoryExists(id))
            {
                return ServiceResponse<bool>.Fail($"Subcategory with id {id} does not exist.", statusCode: 404, errorCode: "subcategory_not_found");
            }

            if (await productRepository.IsSubCategoryInUse(id))
            {
                return ServiceResponse<bool>.Fail("Subcategory is linked to one or more products and cannot be deleted.", statusCode: 400, errorCode: "subcategory_in_use");
            }

            await productRepository.DeleteSubCategory(id);
            return ServiceResponse<bool>.Ok(true, "Subcategory deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to delete subcategory.", ex.ToString(), 500, "subcategory_delete_failed");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTagAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await productRepository.TagExists(id))
            {
                return ServiceResponse<bool>.Fail($"Tag with id {id} does not exist.", statusCode: 404, errorCode: "tag_not_found");
            }

            if (await productRepository.IsTagInUse(id))
            {
                return ServiceResponse<bool>.Fail("Tag is linked to one or more products and cannot be deleted.", statusCode: 400, errorCode: "tag_in_use");
            }

            await productRepository.DeleteTag(id);
            return ServiceResponse<bool>.Ok(true, "Tag deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to delete tag.", ex.ToString(), 500, "tag_delete_failed");
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

    private static string? ValidateTagPayload(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(slug)) return "Slug is required.";
        return null;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
