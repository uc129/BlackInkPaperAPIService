namespace Application.DTOs.Checkout;

public record CreateShippingAddressRequest(
    string FullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    string? Landmark,
    bool IsDefault);

public record UpdateShippingAddressRequest(
    string FullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    string? Landmark,
    bool IsDefault);

public record ShippingAddressDto(
    int Id,
    string FullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    string? Landmark,
    bool IsDefault);

public record CheckoutPreviewRequest(
    int ShippingAddressId,
    string? Notes);

public record CheckoutPreviewDto(
    ShippingAddressDto ShippingAddress,
    string CurrencyCode,
    decimal Subtotal,
    decimal ShippingAmount,
    string ShippingMethod,
    string ShippingLabel,
    decimal TaxAmount,
    string TaxLabel,
    decimal TaxRatePercent,
    decimal TotalAmount,
    IReadOnlyList<CheckoutItemDto> Items);

public record CheckoutItemDto(
    int CartItemId,
    int ProductDbId,
    string ProductId,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Sku,
    string? FulfillmentType,
    IReadOnlyList<CheckoutSelectedVariantDto> SelectedVariants);

public record CheckoutSelectedVariantDto(
    int ProductVariantId,
    int ProductVariantOptionId,
    string VariantLabel,
    string OptionValue);

public record PlaceOrderRequest(
    int ShippingAddressId,
    string? Notes);

public record CreatePaymentSessionRequest(
    int ShippingAddressId,
    string? Notes);

public record PaymentSessionDto(
    int OrderId,
    string OrderNumber,
    string RazorpayOrderId,
    string RazorpayKeyId,
    string CurrencyCode,
    long AmountInSubunits,
    string DisplayName,
    string? DisplayDescription,
    string? PrefillName,
    string? PrefillContact,
    CheckoutPreviewDto Preview);

public record VerifyRazorpayPaymentRequest(
    int OrderId,
    string RazorpayPaymentId,
    string RazorpayOrderId,
    string RazorpaySignature);

public record PlaceOrderResponseDto(
    int OrderId,
    string OrderNumber,
    string Status,
    decimal TotalAmount);

public record OrderDto(
    int Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string? PaymentProvider,
    string CurrencyCode,
    decimal Subtotal,
    decimal ShippingAmount,
    string? ShippingMethod,
    string? ShippingLabel,
    decimal TaxAmount,
    string? TaxLabel,
    decimal? TaxRatePercent,
    decimal TotalAmount,
    string? RazorpayOrderId,
    string? RazorpayPaymentId,
    string? PaymentMethod,
    DateTime? PaidAt,
    string? Notes,
    DateTime CreatedAt,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
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
    IReadOnlyList<OrderSelectedVariantDto> SelectedVariants);

public record OrderSelectedVariantDto(
    int ProductVariantId,
    int ProductVariantOptionId,
    string VariantLabel,
    string OptionValue,
    decimal? PriceModifier,
    decimal? AbsolutePrice,
    string? Sku,
    string? FulfillmentType);
