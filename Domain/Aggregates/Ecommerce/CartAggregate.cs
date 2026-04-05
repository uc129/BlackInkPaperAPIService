using Domain.Entities.Ecommerce;

namespace Domain.Aggregates.Ecommerce;

public class CartAggregate
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CartItemAggregate> Items { get; set; } = [];
}

public class CartItemAggregate
{
    public int Id { get; set; }
    public int CartId { get; set; }
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
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CartItemSelectedVariant> SelectedVariants { get; set; } = [];
}
