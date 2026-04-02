using Application.DTOs.Cart;
using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Mappers;

public static class CartDtoMapper
{
    public static CartResponseDto ToResponse(CartAggregate cart)
    {
        var items = cart.Items
            .Select(item => new CartItemDto(
                item.Id,
                item.ProductDbId,
                item.ProductId,
                item.Name,
                item.Slug,
                item.CoverImageUrl,
                item.CurrencyCode,
                item.BasePrice,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal,
                item.Sku,
                item.FulfillmentType?.ToString(),
                item.SelectedVariants.Select(variant => new CartSelectedVariantDto(
                    variant.ProductVariantId,
                    variant.ProductVariantOptionId,
                    variant.VariantLabel,
                    variant.OptionValue,
                    variant.PriceModifier,
                    variant.AbsolutePrice,
                    variant.Sku,
                    variant.FulfillmentType?.ToString())).ToList()))
            .ToList();

        return new CartResponseDto(
            cart.Id,
            cart.CurrencyCode,
            cart.Status,
            items.Sum(item => item.Quantity),
            items.Sum(item => item.LineTotal),
            cart.UpdatedAt,
            items);
    }
}
