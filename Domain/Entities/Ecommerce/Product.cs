namespace Domain.Entities.Ecommerce
{
    public class ProductTableEntity
    {
        public int Id { get; set; }
        public int ArtistId { get; set; }

        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Grouped Classes
        public ProductTextContent Content { get; set; } = new();
        public ProductPricing Pricing { get; set; } = new();
        public ProductTaxonomy Taxonomy { get; set; } = new();
        public ProductMedia Media { get; set; } = new();
        public ProductStats Stats { get; set; } = new();
        public ProductAudit Audit { get; set; } = new();

        // Domain Specifics
        public int ArtSpecId { get; set; } = new();
        public bool IsUsingStandardVariants { get; set; }

    }
    public record ProductTextContent
    {
        public string NameCode { get; set; } = string.Empty!;
        public string PrintName { get; set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string? ShortDescription { get; private set; } = string.Empty;
    }
    public class ProductPricing
    {
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string CurrencyCode { get; set; } = "INR";
        public int BasePriceLowDenomination => (int)(BasePrice * 100);
        public int FinalPriceLowDenomination => (int)(FinalPrice * 100);
    }
    public class ProductTaxonomy
    {
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsAvailable { get; set; }
    }
    public class ProductMedia
    {
        public string CoverImageUrl { get; set; } = string.Empty;
        public string HeaderImageUrl { get; set; } = string.Empty;
    }
    public class ProductStats
    {
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int? StockQuantity { get; set; }
    }
    public class ProductAudit
    {
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}







//export type ProductType = {
//    id: number;
//product_id: string; // SKU / Business ID
//name: string;
//slug: string;
//artistId: string;
//// --- Descriptive Content ---
//content:
//{
//name_code: string;
//print_name: string;
//description: string;
//    short_description ?: string;
//}
//;

//// --- Pricing (Financial Group) ---
//pricing:
//{
//base_price: number;
//final_price: number;
//currency_code: string;
//base_price_low_denomination: number;
//final_price_low_denomination: number;
//}
//;

//// --- Organization & Hierarchy ---
//taxonomy:
//{
//categoryId: number;
//subCategoryId: number;
//isFeatured: boolean;
//isAvailable: boolean;
//tagIds: number[];
//}
//;
//// --- Media & Assets ---
//media:
//{
//coverImageUrl: string;
//headerImageUrl: string;
//allImageUrls: string[]; // Legacy/Simple
//allProductImages: ProductImageType[]; // Detailed metadata
//}
//;

//// --- Domain Specifics ---
//specs: ArtSpecifications;
//variants: ProductVariant[];
//isUsingStandardVariants: boolean;

//// --- Social & Feedback ---
//stats:
//{
//averageRating: number;
//reviewCount: number;
//    stockQuantity ?: number;
//}
//;

//// --- Audit Metadata ---
//audit:
//{
//createdAt: string;
//createdBy: string;
//updatedAt: string;
//updatedBy: string;
//}
//;
//}
