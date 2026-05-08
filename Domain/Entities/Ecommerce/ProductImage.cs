namespace Domain.Entities.Ecommerce
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public double AspectRatio { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? PlaceholderUrl { get; set; }
        
        // Metadata for digital assets/products
        public string? Format { get; set; }
        public int? Dpi { get; set; }
        public long? FileSize { get; set; }
    }
}
