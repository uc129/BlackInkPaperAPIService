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
    public List<int> TagIds { get; set; } = [];
    public List<ProductImageModel> Images { get; set; } = [];
    public List<ProductVariantModel> Variants { get; set; } = [];
    public int? WeightGrams { get; set; }
    public string? Material { get; set; }
    public string? FileFormat { get; set; }
    public string? PixelDimensions { get; set; }

    public static ProductEditorModel CreateDefault() => new()
    {
        CurrencyCode = "INR",
        IsAvailable = true,
        TagIds = [],
        Images = [],
        Variants = []
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
        TagIds = response.Tags.Select(x => x.Id).ToList(),
        Images = response.Images.Select(ProductImageModel.FromDto).ToList(),
        Variants = response.Variants.Select(ProductVariantModel.FromDto).ToList(),
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
        TagIds,
        Images.Select(x => x.ToCreateDto()).ToList(),
        Variants.Select(x => x.ToCreateDto()).ToList());

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
        TagIds,
        Images.Select(x => x.ToUpdateDto()).ToList(),
        Variants.Select(x => x.ToUpdateDto()).ToList());

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

public class ProductImageModel
{
    public int? Id { get; set; }
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public double AspectRatio { get; set; } = 1.0;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? PlaceholderUrl { get; set; }

    public static ProductImageModel FromDto(ProductImageDto dto) => new()
    {
        Id = dto.Id,
        AltText = dto.AltText,
        IsPrimary = dto.IsPrimary,
        DisplayOrder = dto.DisplayOrder,
        PublicId = dto.PublicId,
        BaseUrl = dto.BaseUrl,
        AspectRatio = dto.AspectRatio,
        Width = dto.Width,
        Height = dto.Height,
        PlaceholderUrl = dto.PlaceholderUrl
    };

    public CreateProductImageDto ToCreateDto() => new(AltText, IsPrimary, DisplayOrder, PublicId, BaseUrl, AspectRatio, Width, Height, PlaceholderUrl);
    public UpdateProductImageDto ToUpdateDto() => new(Id, AltText, IsPrimary, DisplayOrder, PublicId, BaseUrl, AspectRatio, Width, Height, PlaceholderUrl);
}

public class ProductVariantModel
{
    public int? Id { get; set; }
    [Required] public string Label { get; set; } = string.Empty;
    public List<ProductVariantOptionModel> Options { get; set; } = [];

    public static ProductVariantModel FromDto(ProductVariantDto dto) => new()
    {
        Id = dto.Id,
        Label = dto.Label,
        Options = dto.Options.Select(ProductVariantOptionModel.FromDto).ToList()
    };

    public CreateProductVariantDto ToCreateDto() => new(Label, Options.Select(x => x.ToCreateDto()).ToList());
    public UpdateProductVariantDto ToUpdateDto() => new(Id, Label, Options.Select(x => x.ToUpdateDto()).ToList());
}

public class ProductVariantOptionModel
{
    public int? Id { get; set; }
    [Required] public string Value { get; set; } = string.Empty;
    public decimal? PriceModifier { get; set; }
    public decimal? AbsolutePrice { get; set; }
    public int? StockQuantity { get; set; }
    public int FulfillmentType { get; set; } // 0 = digital, 1 = physical
    public string Sku { get; set; } = string.Empty;
    public decimal? WeightGrams { get; set; }

    public static ProductVariantOptionModel FromDto(ProductVariantOptionDto dto) => new()
    {
        Id = dto.Id,
        Value = dto.Value,
        PriceModifier = dto.PriceModifier,
        AbsolutePrice = dto.AbsolutePrice,
        StockQuantity = dto.StockQuantity,
        FulfillmentType = dto.FulfillmentType,
        Sku = dto.Sku,
        WeightGrams = dto.WeightGrams
    };

    public CreateProductVariantOptionDto ToCreateDto() => new(Value, PriceModifier, AbsolutePrice, StockQuantity, FulfillmentType, Sku, WeightGrams);
    public UpdateProductVariantOptionDto ToUpdateDto() => new(Id, Value, PriceModifier, AbsolutePrice, StockQuantity, FulfillmentType, Sku, WeightGrams);
}
