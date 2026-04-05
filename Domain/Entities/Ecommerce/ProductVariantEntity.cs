namespace Domain.Entities.Ecommerce
{
    public class ProductVariantEntity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Label { get; set; } = string.Empty; // e.g., "Format" or "Size"
        public List<int> OptionIds { get; set; } = [];
    }

    public class ProductVariantOption
    {

        /// wether a varinat is physical or digital can be decided from the ProductFulfillmemt Type
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string Value { get; set; } = string.Empty; // e.g., "A3 Print"
        public decimal? PriceModifier { get; set; }
        public decimal? AbsolutePrice { get; set; }
        public int? StockQuantity { get; set; }

        // Hybrid Fulfillment Logic
        public ProductFulfillmentType FulfillmentType { get; set; }  // "digital" | "physical"
        public string Sku { get; set; } = string.Empty;
        public decimal? WeightGrams { get; set; }
    }
}


