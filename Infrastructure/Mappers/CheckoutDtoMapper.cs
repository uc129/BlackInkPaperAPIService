using Application.DTOs.Checkout;
using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Mappers;

public static class CheckoutDtoMapper
{
    public static ShippingAddressDto ToDto(ShippingAddressAggregate address)
        => new(
            address.Id,
            address.FullName,
            address.PhoneNumber,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode,
            address.Landmark,
            address.IsDefault);

    public static OrderDto ToDto(OrderAggregate order)
        => new(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentStatus,
            order.PaymentProvider,
            order.CurrencyCode,
            order.Subtotal,
            order.ShippingAmount,
            order.ShippingMethod,
            order.ShippingLabel,
            order.TaxAmount,
            order.TaxLabel,
            order.TaxRatePercent,
            order.TotalAmount,
            order.RazorpayOrderId,
            order.RazorpayPaymentId,
            order.PaymentMethod,
            order.PaidAt,
            order.Notes,
            order.CreatedAt,
            ToDto(order.ShippingAddress),
            order.Items.Select(item => new OrderItemDto(
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
                item.FulfillmentType,
                item.SelectedVariants.Select(variant => new OrderSelectedVariantDto(
                    variant.ProductVariantId,
                    variant.ProductVariantOptionId,
                    variant.VariantLabel,
                    variant.OptionValue,
                    variant.PriceModifier,
                    variant.AbsolutePrice,
                    variant.Sku,
                    variant.FulfillmentType)).ToList())).ToList());

    public static ShippingAddressAggregate ToAggregate(string userId, CreateShippingAddressRequest request, DateTime nowUtc)
        => new()
        {
            UserId = userId,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            Landmark = request.Landmark?.Trim(),
            IsDefault = request.IsDefault,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

    public static void ApplyUpdate(ShippingAddressAggregate address, UpdateShippingAddressRequest request, DateTime nowUtc)
    {
        address.FullName = request.FullName.Trim();
        address.PhoneNumber = request.PhoneNumber.Trim();
        address.AddressLine1 = request.AddressLine1.Trim();
        address.AddressLine2 = request.AddressLine2?.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        address.Landmark = request.Landmark?.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = nowUtc;
    }
}
