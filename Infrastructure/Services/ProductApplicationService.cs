using Application.DTOs.Products;
using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Mappers;

namespace Infrastructure.Services;

public class ProductApplicationService(IProductRepository productRepository) : IProductApplicationService
{
    public async Task<ServiceResponse<ProductResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await productRepository.GetById(id);
            if (product is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail($"Product with id {id} was not found.", statusCode: 404, errorCode: "product_not_found");
            }

            return ServiceResponse<ProductResponseDto>.Ok(ProductDtoMapper.ToResponse(product));
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Unable to fetch product.", ex.ToString(), 500, "product_read_failed");
        }
    }

    public async Task<ServiceResponse<ProductResponseDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return ServiceResponse<ProductResponseDto>.Fail("Slug is required.", statusCode: 400, errorCode: "validation_error");
            }

            var product = await productRepository.GetBySlug(slug.Trim());
            if (product is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail($"Product with slug '{slug}' was not found.", statusCode: 404, errorCode: "product_not_found");
            }

            return ServiceResponse<ProductResponseDto>.Ok(ProductDtoMapper.ToResponse(product));
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Unable to fetch product by slug.", ex.ToString(), 500, "product_read_failed");
        }
    }

    public async Task<ServiceResponse<PagedResultDto<ProductSummaryDto>>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPage = Math.Max(request.Page, 1);
            var normalizedPageSize = Math.Clamp(request.PageSize, 1, 100);

            var (items, totalCount) = await productRepository.Search(
                request.Query,
                request.ArtistId,
                request.CategoryId,
                request.SubCategoryId,
                request.TagId,
                request.IsAvailable,
                request.IsFeatured,
                normalizedPage,
                normalizedPageSize);

            var result = new PagedResultDto<ProductSummaryDto>(
                items.Select(ProductDtoMapper.ToSummary).ToList(),
                normalizedPage,
                normalizedPageSize,
                totalCount);

            return ServiceResponse<PagedResultDto<ProductSummaryDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResultDto<ProductSummaryDto>>.Fail("Unable to search products.", ex.ToString(), 500, "product_search_failed");
        }
    }

    public async Task<ServiceResponse<ProductResponseDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = await ValidateForCreate(request);
            if (!validation.Success)
            {
                return validation;
            }

            var aggregate = ProductDtoMapper.ToNewAggregate(request, DateTime.UtcNow, "system");

            aggregate.Id = await productRepository.Add(aggregate);

            var saved = await productRepository.GetById(aggregate.Id);
            return saved is null ? ServiceResponse<ProductResponseDto>.Fail("Product was created but could not be reloaded.", statusCode: 500, errorCode: "product_reload_failed")
                : ServiceResponse<ProductResponseDto>.Ok(ProductDtoMapper.ToResponse(saved), "Product created successfully.", 201);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Unable to create product.", ex.ToString(), 500, "product_create_failed");
        }
    }

    public async Task<ServiceResponse<ProductResponseDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await productRepository.GetById(id);
            if (existing is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail($"Product with id {id} was not found.", statusCode: 404, errorCode: "product_not_found");
            }

            var validation = await ValidateForUpdate(id, request);
            if (!validation.Success)
            {
                return validation;
            }

            ProductDtoMapper.ApplyUpdate(existing, request, DateTime.UtcNow, "system");

            await productRepository.Update(existing);

            var updated = await productRepository.GetById(id);
            if (updated is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail("Product was updated but could not be reloaded.", statusCode: 500, errorCode: "product_reload_failed");
            }

            return ServiceResponse<ProductResponseDto>.Ok(ProductDtoMapper.ToResponse(updated), "Product updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Unable to update product.", ex.ToString(), 500, "product_update_failed");
        }
    }

    public async Task<ServiceResponse<ProductResponseDto>> UpdateFlagsAsync(int id, UpdateProductFlagsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.IsAvailable is null && request.IsFeatured is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail(
                    "At least one flag value must be provided.",
                    statusCode: 400,
                    errorCode: "validation_error");
            }

            var existing = await productRepository.GetById(id);
            if (existing is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail(
                    $"Product with id {id} was not found.",
                    statusCode: 404,
                    errorCode: "product_not_found");
            }

            await productRepository.UpdateFlags(id, request.IsAvailable, request.IsFeatured, DateTime.UtcNow, "system");

            var updated = await productRepository.GetById(id);
            if (updated is null)
            {
                return ServiceResponse<ProductResponseDto>.Fail(
                    "Product flags were updated but the product could not be reloaded.",
                    statusCode: 500,
                    errorCode: "product_reload_failed");
            }

            return ServiceResponse<ProductResponseDto>.Ok(ProductDtoMapper.ToResponse(updated), "Product flags updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Unable to update product flags.", ex.ToString(), 500, "product_update_failed");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await productRepository.GetById(id);
            if (existing is null)
            {
                return ServiceResponse<bool>.Fail($"Product with id {id} was not found.", statusCode: 404, errorCode: "product_not_found");
            }

            await productRepository.Delete(existing);
            return ServiceResponse<bool>.Ok(true, "Product deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to delete product.", ex.ToString(), 500, "product_delete_failed");
        }
    }

    private async Task<ServiceResponse<ProductResponseDto>> ValidateForCreate(CreateProductRequest request)
    {
        var basicValidation = ValidateCommonFields(
            request.ProductId,
            request.Name,
            request.Slug,
            request.Content.NameCode,
            request.Pricing.BasePrice,
            request.Pricing.FinalPrice,
            request.Pricing.CurrencyCode);

        if (!basicValidation.Success)
        {
            return basicValidation;
        }

        if (!await productRepository.ArtistExists(request.ArtistId))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"Artist with id {request.ArtistId} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        if (!await productRepository.CategoryExists(request.CategoryId))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"Category with id {request.CategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        if (!await productRepository.SubCategoryExists(request.SubCategoryId))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"SubCategory with id {request.SubCategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        var artSpecificationValidation = await ValidateArtSpecification(request.ArtSpecId, request.ArtSpecs);
        if (!artSpecificationValidation.Success)
        {
            return artSpecificationValidation;
        }

        if (await productRepository.ExistsBySlug(request.Slug.Trim()))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"Slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        if (await productRepository.ExistsByProductId(request.ProductId.Trim()))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"ProductId '{request.ProductId}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        if (await productRepository.ExistsByNameCode(request.Content.NameCode.Trim()))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"NameCode '{request.Content.NameCode}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        return ServiceResponse<ProductResponseDto>.Ok(null!, "Validation passed.");
    }

    private async Task<ServiceResponse<ProductResponseDto>> ValidateForUpdate(int id, UpdateProductRequest request)
    {
        var basicValidation = ValidateCommonFields(
            request.ProductId,
            request.Name,
            request.Slug,
            request.Content.NameCode,
            request.Pricing.BasePrice,
            request.Pricing.FinalPrice,
            request.Pricing.CurrencyCode);

        if (!basicValidation.Success)
        {
            return basicValidation;
        }

        if (!await productRepository.CategoryExists(request.CategoryId))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"Category with id {request.CategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        if (!await productRepository.SubCategoryExists(request.SubCategoryId))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"SubCategory with id {request.SubCategoryId} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        var artSpecificationValidation = await ValidateArtSpecification(request.ArtSpecId, request.ArtSpecs);
        if (!artSpecificationValidation.Success)
        {
            return artSpecificationValidation;
        }

        if (await productRepository.ExistsBySlug(request.Slug.Trim(), id))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"Slug '{request.Slug}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        if (await productRepository.ExistsByProductId(request.ProductId.Trim(), id))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"ProductId '{request.ProductId}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        if (await productRepository.ExistsByNameCode(request.Content.NameCode.Trim(), id))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"NameCode '{request.Content.NameCode}' already exists.", statusCode: 400, errorCode: "validation_error");
        }

        return ServiceResponse<ProductResponseDto>.Ok(null!, "Validation passed.");
    }

    private static ServiceResponse<ProductResponseDto> ValidateCommonFields(
        string productId,
        string name,
        string slug,
        string nameCode,
        decimal basePrice,
        decimal finalPrice,
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return ServiceResponse<ProductResponseDto>.Fail("ProductId is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResponse<ProductResponseDto>.Fail("Name is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return ServiceResponse<ProductResponseDto>.Fail("Slug is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (string.IsNullOrWhiteSpace(nameCode))
        {
            return ServiceResponse<ProductResponseDto>.Fail("NameCode is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return ServiceResponse<ProductResponseDto>.Fail("CurrencyCode is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (basePrice < 0 || finalPrice < 0)
        {
            return ServiceResponse<ProductResponseDto>.Fail("Pricing values cannot be negative.", statusCode: 400, errorCode: "validation_error");
        }

        return ServiceResponse<ProductResponseDto>.Ok(null!, "Validation passed.");
    }

    private async Task<ServiceResponse<ProductResponseDto>> ValidateArtSpecification(int? artSpecId, ArtSpecificationsDto? artSpecs)
    {
        if (artSpecId is null && artSpecs is null)
        {
            return ServiceResponse<ProductResponseDto>.Fail("ArtSpecs or ArtSpecId is required.", statusCode: 400, errorCode: "validation_error");
        }

        if (artSpecs is null && artSpecId.HasValue && !await productRepository.ArtSpecificationExists(artSpecId.Value))
        {
            return ServiceResponse<ProductResponseDto>.Fail($"ArtSpecification with id {artSpecId.Value} does not exist.", statusCode: 400, errorCode: "validation_error");
        }

        return ServiceResponse<ProductResponseDto>.Ok(null!, "Validation passed.");
    }
}
