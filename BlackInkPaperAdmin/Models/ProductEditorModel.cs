using System.ComponentModel.DataAnnotations;
using Application.DTOs.Products;

namespace BlackInkPaperAdmin.Models;

public class ProductEditorModel
{
    [Required] public string ProductId { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Slug { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int ArtistId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(1, int.MaxValue)] public int SubCategoryId { get; set; }
    public int? ArtSpecId { get; set; }
    public bool IsUsingStandardVariants { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsAvailable { get; set; }
    [Required] public string NameCode { get; set; } = string.Empty;
    [Required] public string PrintName { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    [Required] public string CurrencyCode { get; set; } = "INR";
    public string CoverImageUrl { get; set; } = string.Empty;
    public string HeaderImageUrl { get; set; } = string.Empty;
    public int? StockQuantity { get; set; }
    public int? WeightGrams { get; set; }
    public string? Material { get; set; }
    public string? FileFormat { get; set; }
    public string? PixelDimensions { get; set; }

    public static ProductEditorModel CreateDefault() => new()
    {
        CurrencyCode = "INR",
        IsAvailable = true
    };

    public static ProductEditorModel FromResponse(ProductResponseDto response) => new()
    {
        ProductId = response.ProductId,
        Name = response.Name,
        Slug = response.Slug,
        ArtistId = response.ArtistId,
        CategoryId = response.Taxonomy.CategoryId,
        SubCategoryId = response.Taxonomy.SubCategoryId,
        ArtSpecId = response.ArtSpecId,
        IsUsingStandardVariants = response.IsUsingStandardVariants,
        IsFeatured = response.Taxonomy.IsFeatured,
        IsAvailable = response.Taxonomy.IsAvailable,
        NameCode = response.Content.NameCode,
        PrintName = response.Content.PrintName,
        Description = response.Content.Description,
        ShortDescription = response.Content.ShortDescription,
        BasePrice = response.Pricing.BasePrice,
        FinalPrice = response.Pricing.FinalPrice,
        CurrencyCode = response.Pricing.CurrencyCode,
        CoverImageUrl = response.Media.CoverImageUrl,
        HeaderImageUrl = response.Media.HeaderImageUrl,
        StockQuantity = response.Stats.StockQuantity,
        WeightGrams = response.ArtSpecs?.WeightGrams,
        Material = response.ArtSpecs?.Material,
        FileFormat = response.ArtSpecs?.FileFormat,
        PixelDimensions = response.ArtSpecs?.PixelDimensions
    };

    public CreateProductRequest ToCreateRequest() => new(
        ProductId,
        Name,
        Slug,
        ArtistId,
        CategoryId,
        SubCategoryId,
        ArtSpecId,
        BuildArtSpecs(),
        IsUsingStandardVariants,
        IsFeatured,
        IsAvailable,
        new ProductTextContentDto(NameCode, PrintName, Description, ShortDescription),
        new ProductPricingDto(BasePrice, FinalPrice, CurrencyCode),
        new ProductMediaDto(CoverImageUrl, HeaderImageUrl),
        StockQuantity,
        null,
        null,
        null);

    public UpdateProductRequest ToUpdateRequest() => new(
        ProductId,
        Name,
        Slug,
        CategoryId,
        SubCategoryId,
        ArtSpecId,
        BuildArtSpecs(),
        IsUsingStandardVariants,
        IsFeatured,
        IsAvailable,
        new ProductTextContentDto(NameCode, PrintName, Description, ShortDescription),
        new ProductPricingDto(BasePrice, FinalPrice, CurrencyCode),
        new ProductMediaDto(CoverImageUrl, HeaderImageUrl),
        StockQuantity,
        null,
        null,
        null);

    private ArtSpecificationsDto? BuildArtSpecs()
    {
        if (WeightGrams is null &&
            string.IsNullOrWhiteSpace(Material) &&
            string.IsNullOrWhiteSpace(FileFormat) &&
            string.IsNullOrWhiteSpace(PixelDimensions))
        {
            return null;
        }

        return new ArtSpecificationsDto(null, WeightGrams, null, Material, FileFormat, null, PixelDimensions);
    }
}
