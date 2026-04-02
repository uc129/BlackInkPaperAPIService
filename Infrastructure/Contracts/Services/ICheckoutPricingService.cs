using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Services;

public interface ICheckoutPricingService
{
    Task<CheckoutPricingResult> BuildAsync(CartAggregate cart, ShippingAddressAggregate shippingAddress, CancellationToken cancellationToken = default);
}

public sealed class CheckoutPricingResult
{
    public string CurrencyCode { get; init; } = "INR";
    public decimal Subtotal { get; init; }
    public decimal ShippingAmount { get; init; }
    public string ShippingMethod { get; init; } = string.Empty;
    public string ShippingLabel { get; init; } = string.Empty;
    public decimal TaxAmount { get; init; }
    public string TaxLabel { get; init; } = "GST";
    public decimal TaxRatePercent { get; init; }
    public decimal TotalAmount { get; init; }
    public List<CheckoutPricingLine> Lines { get; init; } = [];
}

public sealed class CheckoutPricingLine
{
    public required CartItemAggregate CartItem { get; init; }
    public required ProductAggregate Product { get; init; }
    public required decimal BasePrice { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal LineTotal { get; init; }
    public required bool RequiresShipping { get; init; }
    public required decimal WeightGrams { get; init; }
    public required string? Sku { get; init; }
    public required string? FulfillmentType { get; init; }
}
