namespace Domain.Entities.Ecommerce;

public class CartItemSelectedVariant
{
    public int Id { get; set; }
    public int CartItemId { get; set; }
    public int ProductVariantId { get; set; }
    public int ProductVariantOptionId { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public string OptionValue { get; set; } = string.Empty;
    public decimal? PriceModifier { get; set; }
    public decimal? AbsolutePrice { get; set; }
    public string? Sku { get; set; }
    public ProductFulfillmentType? FulfillmentType { get; set; }
}
