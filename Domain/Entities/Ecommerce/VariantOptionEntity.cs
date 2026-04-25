namespace Domain.Entities.Ecommerce;


public class ProductVariantOption
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public string Value { get; set; } = string.Empty; // e.g., "Cotton Rag Paper"
    public decimal? PriceModifier { get; set; }
    public decimal? AbsolutePrice { get; set; }
    public int? StockQuantity { get; set; }
}

public class PhysicalVariantOption : ProductVariantOption
{
    // Add physical specific options if needed in future
}

public class DigitalVariantOption : ProductVariantOption
{
    // Add digital specific options if needed in future
}