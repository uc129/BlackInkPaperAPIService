namespace Domain.Entities.Ecommerce
{
    public class Dimensions
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public DimensionUnits Unit { get; set; } = DimensionUnits.cm; // "cm" | "in"
    }
    public enum DimensionUnits
    {
        cm,
        inch,
        feet,
        mm
    }
}
