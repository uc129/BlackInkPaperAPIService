using Domain.Entities.Ecommerce;

namespace Domain.Aggregates.Ecommerce;

public class OrderAggregate
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int ShippingAddressId { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = "PendingPayment";
    public string PaymentStatus { get; set; } = "Pending";
    public string? PaymentProvider { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentFailureReason { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingAmount { get; set; }
    public string? ShippingMethod { get; set; }
    public string? ShippingLabel { get; set; }
    public decimal TaxAmount { get; set; }
    public string? TaxLabel { get; set; }
    public decimal? TaxRatePercent { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ShippingAddressAggregate ShippingAddress { get; set; } = new();
    public List<OrderItemAggregate> Items { get; set; } = [];
    // populated only in admin listing queries
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public int? ItemCount { get; set; }
}

public class OrderItemAggregate
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductDbId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public decimal BasePrice { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? Sku { get; set; }
    public ProductFulfillmentType? FulfillmentType { get; set; }
    public List<OrderItemSelectedVariantAggregate> SelectedVariants { get; set; } = [];
}

public class OrderItemSelectedVariantAggregate
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public int ProductVariantId { get; set; }
    public int ProductVariantOptionId { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public string OptionValue { get; set; } = string.Empty;
    public decimal? PriceModifier { get; set; }
    public decimal? AbsolutePrice { get; set; }
    public string? Sku { get; set; }
    public ProductFulfillmentType? FulfillmentType { get; set; }
}