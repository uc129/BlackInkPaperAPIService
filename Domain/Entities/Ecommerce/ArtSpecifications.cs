namespace Domain.Entities.Ecommerce
{
    public class ArtSpecifications
    {
        // Physical Art Details
        public Dimensions? PhysicalDimensions { get; set; }
        public int? WeightGrams { get; set; }
        public bool? IsFramed { get; set; }
        public string? Material { get; set; }

        // Digital Art Details
        public string? FileFormat { get; set; }
        public int? ResolutionDpi { get; set; }
        public string? PixelDimensions { get; set; }
        public string? PaperType { get; set; }
        public string? PaperWeight { get; set; }
        public string? InkType { get; set; }
        public bool IsOriginal { get; set; }
        public bool IsSigned { get; set; }
        public bool HasCertificate { get; set; }
        public string? FramingStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}