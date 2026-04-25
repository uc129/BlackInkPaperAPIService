using Domain.Entities.Ecommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.Ecommerce
{
    public class ProductVariantAggregate
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Label { get; set; } = string.Empty; // e.g., "A3 Physical Print"
        public ProductFulfillmentType FulfillmentType { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal? WeightGrams { get; set; }
        public int? StockQuantity { get; set; }
        public decimal? AbsolutePrice { get; set; }
        public List<ProductVariantOption> Options { get; set; } = [];
    }
}
