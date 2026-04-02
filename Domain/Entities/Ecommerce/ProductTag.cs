namespace Domain.Entities.Ecommerce
{
    public class ProductTag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string Slug { get; set; } = string.Empty;
    }
}
