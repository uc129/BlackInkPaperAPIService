using Domain.Entities.Ecommerce;

namespace Domain.Aggregates.Ecommerce
{
    public class ProductAggregate
    {
        public int Id { get; set; }
        public int ArtistId { get; set; }
        public string ProductId { get; set; } = string.Empty; // Business SKU
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Content
        public string NameCode { get; set; } = string.Empty;
        public string PrintName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }

        // Pricing
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string CurrencyCode { get; set; } = "INR";

        // Taxonomy
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsAvailable { get; set; }

        // Domain Specifics 
        public bool IsUsingStandardVariants { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public string HeaderImageUrl { get; set; } = string.Empty;

        // Stats
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int? StockQuantity { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;


        // Navigation Properties (For Dapper Mapping)
        public ArtSpecifications ArtSpecs { get; set; } = new();
        public List<ProductImage> Images { get; set; } = [];
        public List<ProductVariantAggregate> Variants { get; set; } = [];
        public List<ProductTag> Tags { get; set; } = [];
    }
}
