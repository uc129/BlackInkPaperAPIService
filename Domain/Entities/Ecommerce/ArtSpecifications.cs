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
    }


}
