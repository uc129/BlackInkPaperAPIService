using Application.DTOs.Cart;
using Common.YourProject.Models;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Mappers;

namespace Infrastructure.Services;

public class CartApplicationService(
    ICartRepository cartRepository,
    IProductRepository productRepository) : ICartApplicationService
{
    public async Task<ServiceResponse<CartResponseDto>> GetActiveCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResponse<CartResponseDto>.Fail("User id is required.", statusCode: 401, errorCode: "unauthorized");
            }

            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null)
            {
                return ServiceResponse<CartResponseDto>.Ok(
                    new CartResponseDto(0, "INR", "Active", 0, 0, DateTime.UtcNow, []));
            }

            return ServiceResponse<CartResponseDto>.Ok(CartDtoMapper.ToResponse(cart));
        }
        catch (Exception ex)
        {
            return ServiceResponse<CartResponseDto>.Fail("Unable to fetch cart.", ex.ToString(), 500, "cart_read_failed");
        }
    }

    public async Task<ServiceResponse<CartResponseDto>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResponse<CartResponseDto>.Fail("User id is required.", statusCode: 401, errorCode: "unauthorized");
            }

            if (request.Quantity <= 0)
            {
                return ServiceResponse<CartResponseDto>.Fail("Quantity must be greater than zero.", statusCode: 400, errorCode: "validation_error");
            }

            var product = await productRepository.GetById(request.ProductDbId);
            if (product is null || !product.IsAvailable)
            {
                return ServiceResponse<CartResponseDto>.Fail("Product is not available.", statusCode: 404, errorCode: "product_not_found");
            }

            var selectedVariants = ResolveSelectedVariants(product, request.SelectedVariants);
            if (selectedVariants is null)
            {
                return ServiceResponse<CartResponseDto>.Fail("One or more selected variants are invalid.", statusCode: 400, errorCode: "validation_error");
            }

            var unitPrice = CalculateUnitPrice(product.FinalPrice, selectedVariants);
            var sku = selectedVariants.LastOrDefault(variant => !string.IsNullOrWhiteSpace(variant.Sku))?.Sku;
            var fulfillmentType = selectedVariants.LastOrDefault(variant => variant.FulfillmentType.HasValue)?.FulfillmentType;
            var now = DateTime.UtcNow;

            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null)
            {
                var cartId = await cartRepository.CreateCart(userId, product.CurrencyCode, now);
                cart = new CartAggregate
                {
                    Id = cartId,
                    UserId = userId,
                    CurrencyCode = product.CurrencyCode,
                    Status = "Active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Items = []
                };
            }

            var existingItem = cart.Items.FirstOrDefault(item => IsSameConfiguration(item, product.Id, selectedVariants));
            if (existingItem is not null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.UnitPrice = unitPrice;
                existingItem.LineTotal = unitPrice * existingItem.Quantity;
                existingItem.UpdatedAt = now;
                await cartRepository.UpdateItem(cart.Id, existingItem);
            }
            else
            {
                var newItem = new CartItemAggregate
                {
                    CartId = cart.Id,
                    ProductDbId = product.Id,
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Slug = product.Slug,
                    CoverImageUrl = product.CoverImageUrl,
                    CurrencyCode = product.CurrencyCode,
                    BasePrice = product.BasePrice,
                    UnitPrice = unitPrice,
                    Quantity = request.Quantity,
                    LineTotal = unitPrice * request.Quantity,
                    Sku = sku,
                    FulfillmentType = fulfillmentType,
                    AddedAt = now,
                    UpdatedAt = now,
                    SelectedVariants = selectedVariants
                };

                await cartRepository.AddItem(cart.Id, newItem);
            }

            var updatedCart = await cartRepository.GetActiveCart(userId)
                ?? new CartAggregate { UserId = userId, CurrencyCode = product.CurrencyCode, Status = "Active", UpdatedAt = now };

            return ServiceResponse<CartResponseDto>.Ok(CartDtoMapper.ToResponse(updatedCart), "Cart updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CartResponseDto>.Fail("Unable to add item to cart.", ex.ToString(), 500, "cart_write_failed");
        }
    }

    public async Task<ServiceResponse<CartResponseDto>> UpdateItemQuantityAsync(string userId, int cartItemId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null)
            {
                return ServiceResponse<CartResponseDto>.Fail("Cart not found.", statusCode: 404, errorCode: "cart_not_found");
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item is null)
            {
                return ServiceResponse<CartResponseDto>.Fail("Cart item not found.", statusCode: 404, errorCode: "cart_item_not_found");
            }

            if (request.Quantity <= 0)
            {
                await cartRepository.RemoveItem(cart.Id, cartItemId);
            }
            else
            {
                await cartRepository.UpdateItemQuantity(cart.Id, cartItemId, request.Quantity, item.UnitPrice * request.Quantity, DateTime.UtcNow);
            }

            var updatedCart = await cartRepository.GetActiveCart(userId)
                ?? new CartAggregate { UserId = userId, CurrencyCode = cart.CurrencyCode, Status = "Active", UpdatedAt = DateTime.UtcNow };

            return ServiceResponse<CartResponseDto>.Ok(CartDtoMapper.ToResponse(updatedCart), "Cart updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CartResponseDto>.Fail("Unable to update cart item quantity.", ex.ToString(), 500, "cart_write_failed");
        }
    }

    public async Task<ServiceResponse<CartResponseDto>> RemoveItemAsync(string userId, int cartItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null)
            {
                return ServiceResponse<CartResponseDto>.Fail("Cart not found.", statusCode: 404, errorCode: "cart_not_found");
            }

            await cartRepository.RemoveItem(cart.Id, cartItemId);

            var updatedCart = await cartRepository.GetActiveCart(userId)
                ?? new CartAggregate { UserId = userId, CurrencyCode = cart.CurrencyCode, Status = "Active", UpdatedAt = DateTime.UtcNow };

            return ServiceResponse<CartResponseDto>.Ok(CartDtoMapper.ToResponse(updatedCart), "Cart updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CartResponseDto>.Fail("Unable to remove cart item.", ex.ToString(), 500, "cart_write_failed");
        }
    }

    public async Task<ServiceResponse<CartResponseDto>> ClearAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null)
            {
                return ServiceResponse<CartResponseDto>.Ok(new CartResponseDto(0, "INR", "Active", 0, 0, DateTime.UtcNow, []));
            }

            await cartRepository.ClearCart(cart.Id);

            var updatedCart = await cartRepository.GetActiveCart(userId)
                ?? new CartAggregate { Id = cart.Id, UserId = userId, CurrencyCode = cart.CurrencyCode, Status = "Active", UpdatedAt = DateTime.UtcNow };

            return ServiceResponse<CartResponseDto>.Ok(CartDtoMapper.ToResponse(updatedCart), "Cart cleared successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CartResponseDto>.Fail("Unable to clear cart.", ex.ToString(), 500, "cart_write_failed");
        }
    }

    private static bool IsSameConfiguration(CartItemAggregate existingItem, int productDbId, IReadOnlyCollection<CartItemSelectedVariant> selectedVariants)
    {
        if (existingItem.ProductDbId != productDbId)
        {
            return false;
        }

        var existingOptionIds = existingItem.SelectedVariants
            .Select(variant => variant.ProductVariantOptionId)
            .OrderBy(id => id)
            .ToArray();

        var incomingOptionIds = selectedVariants
            .Select(variant => variant.ProductVariantOptionId)
            .OrderBy(id => id)
            .ToArray();

        return existingOptionIds.SequenceEqual(incomingOptionIds);
    }

    private static List<CartItemSelectedVariant>? ResolveSelectedVariants(
        ProductAggregate product,
        IReadOnlyCollection<AddCartItemSelectedVariantRequest>? selectedVariants)
    {
        if (selectedVariants is null || selectedVariants.Count == 0)
        {
            return [];
        }

        var resolved = new List<CartItemSelectedVariant>();
        foreach (var selectedVariant in selectedVariants)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == selectedVariant.ProductVariantId);
            var option = variant?.Options.FirstOrDefault(o => o.Id == selectedVariant.ProductVariantOptionId);
            if (variant is null || option is null)
            {
                return null;
            }

            resolved.Add(new CartItemSelectedVariant
            {
                ProductVariantId = variant.Id,
                ProductVariantOptionId = option.Id,
                VariantLabel = variant.Label,
                OptionValue = option.Value,
                PriceModifier = option.PriceModifier,
                AbsolutePrice = option.AbsolutePrice,
                Sku = option.Sku,
                FulfillmentType = option.FulfillmentType
            });
        }

        return resolved;
    }

    private static decimal CalculateUnitPrice(decimal productPrice, IReadOnlyCollection<CartItemSelectedVariant> selectedVariants)
    {
        var unitPrice = productPrice;
        var absoluteSelections = selectedVariants.Where(variant => variant.AbsolutePrice.HasValue).ToList();

        if (absoluteSelections.Count == 1)
        {
            unitPrice = absoluteSelections[0].AbsolutePrice!.Value;
        }

        foreach (var selectedVariant in selectedVariants.Where(variant => variant.PriceModifier.HasValue))
        {
            unitPrice += selectedVariant.PriceModifier!.Value;
        }

        return unitPrice;
    }
}
