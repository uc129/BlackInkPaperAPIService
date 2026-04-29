namespace BlackInkPaperAdmin.Components.Pages.Products;

public sealed class ProductFormViewModel
{
    public string Name        { get; set; } = "";
    public string ProductId   { get; set; } = "";
    public string Slug        { get; set; } = "";
    public string PrintName   { get; set; } = "";
    public string NameCode    { get; set; } = "";
    public string ShortDesc   { get; set; } = "";
    public string Description { get; set; } = "";

    public decimal BasePrice  { get; set; }
    public decimal FinalPrice { get; set; }
    public string  Currency   { get; set; } = "INR";

    public string  Material    { get; set; } = "";
    public string  FileFormat  { get; set; } = "";
    public string  PixelDims   { get; set; } = "";
    public int?    WeightGrams { get; set; }
    public bool?   IsFramed    { get; set; }
    public double? DimW        { get; set; }
    public double? DimH        { get; set; }
    public string  DimUnit     { get; set; } = "cm";
    public int?    ResDpi      { get; set; }

    public string CoverUrl  { get; set; } = "";
    public string HeaderUrl { get; set; } = "";

    public bool UseStdVariants { get; set; } = true;
    public List<VariantGroupModel> Variants { get; set; } = new();

    public int  ArtistId      { get; set; }
    public int  CategoryId    { get; set; }
    public int  SubCategoryId { get; set; }
    public bool IsAvailable   { get; set; } = true;
    public bool IsFeatured    { get; set; }
    public int? Stock         { get; set; }
    public List<int> SelectedTagIds { get; set; } = new();
}

public sealed class VariantGroupModel
{
    public string Label       { get; set; } = "";
    public string Sku         { get; set; } = "";
    public int    FulfillType { get; set; } = 1;
    public List<VariantOptionModel> Options { get; set; } = new();
}

public sealed class VariantOptionModel
{
    public string   Value    { get; set; } = "";
    public decimal? PriceMod { get; set; }
    public decimal? AbsPrice { get; set; }
    public int?     Stock    { get; set; }
}
