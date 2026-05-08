namespace Domain.Entities.Ecommerce
{
    public class ProductVariantEntity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Label { get; set; } = string.Empty; // e.g., "A3 Physical Print"
        public ProductFulfillmentType FulfillmentType { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal? WeightGrams { get; set; }
        public int? StockQuantity { get; set; }
        public decimal? AbsolutePrice { get; set; }
        public int? ProductImageId { get; set; } // Link to specific asset (e.g. for digital products)
        public List<int> OptionIds { get; set; } = [];
    }
    
}


