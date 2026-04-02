namespace Application.DTOs.Cart;

public record AddCartItemRequest(
    int ProductDbId,
    int Quantity,
    List<AddCartItemSelectedVariantRequest>? SelectedVariants);

public record AddCartItemSelectedVariantRequest(
    int ProductVariantId,
    int ProductVariantOptionId);

public record UpdateCartItemQuantityRequest(int Quantity);

public record CartResponseDto(
    int Id,
    string CurrencyCode,
    string Status,
    int ItemCount,
    decimal Subtotal,
    DateTime UpdatedAt,
    IReadOnlyList<CartItemDto> Items);

public record CartItemDto(
    int Id,
    int ProductDbId,
    string ProductId,
    string Name,
    string Slug,
    string? CoverImageUrl,
    string CurrencyCode,
    decimal BasePrice,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Sku,
    string? FulfillmentType,
    IReadOnlyList<CartSelectedVariantDto> SelectedVariants);

public record CartSelectedVariantDto(
    int ProductVariantId,
    int ProductVariantOptionId,
    string VariantLabel,
    string OptionValue,
    decimal? PriceModifier,
    decimal? AbsolutePrice,
    string? Sku,
    string? FulfillmentType);
