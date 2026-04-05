using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class CheckoutPricingService(
    IProductRepository productRepository,
    IOptions<CheckoutPricingOptions> optionsAccessor) : ICheckoutPricingService
{
    private readonly CheckoutPricingOptions options = optionsAccessor.Value;

    public async Task<CheckoutPricingResult> BuildAsync(CartAggregate cart, ShippingAddressAggregate shippingAddress, CancellationToken cancellationToken = default)
    {
        var lines = new List<CheckoutPricingLine>(cart.Items.Count);

        foreach (var item in cart.Items)
        {
            var product = await productRepository.GetById(item.ProductDbId);
            if (product is null || !product.IsAvailable)
            {
                throw new InvalidOperationException($"Product '{item.Name}' is no longer available.");
            }

            if (!string.Equals(product.CurrencyCode, cart.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Product '{item.Name}' has a different currency than the active cart.");
            }

            var basePrice = product.FinalPrice > 0 ? product.FinalPrice : product.BasePrice;
            var unitPrice = basePrice;
            var selectedSku = item.Sku;
            var requiresShipping = item.FulfillmentType == ProductFulfillmentType.physical;
            decimal weightGrams = product.ArtSpecs.WeightGrams ?? 0m;

            foreach (var selectedVariant in item.SelectedVariants)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == selectedVariant.ProductVariantId);
                var option = variant?.Options.FirstOrDefault(o => o.Id == selectedVariant.ProductVariantOptionId);
                if (variant is null || option is null)
                {
                    throw new InvalidOperationException($"A selected variant for '{item.Name}' is invalid.");
                }

                if (option.AbsolutePrice.HasValue)
                {
                    unitPrice = option.AbsolutePrice.Value;
                }
                else if (option.PriceModifier.HasValue)
                {
                    unitPrice += option.PriceModifier.Value;
                }

                if (option.StockQuantity.HasValue && item.Quantity > option.StockQuantity.Value)
                {
                    throw new InvalidOperationException($"Insufficient stock for selected variant of '{item.Name}'.");
                }

                selectedSku = string.IsNullOrWhiteSpace(option.Sku) ? selectedSku : option.Sku;
                requiresShipping |= option.FulfillmentType == ProductFulfillmentType.physical;
                weightGrams += option.WeightGrams ?? 0m;
            }

            if (item.SelectedVariants.Count == 0 && product.StockQuantity.HasValue && item.Quantity > product.StockQuantity.Value)
            {
                throw new InvalidOperationException($"Insufficient stock for '{item.Name}'.");
            }

            var lineTotal = unitPrice * item.Quantity;

            lines.Add(new CheckoutPricingLine
            {
                CartItem = item,
                Product = product,
                BasePrice = basePrice,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                RequiresShipping = requiresShipping,
                WeightGrams = weightGrams,
                Sku = selectedSku,
                FulfillmentType = requiresShipping ? ProductFulfillmentType.physical : ProductFulfillmentType.digital
            });
        }

        var subtotal = lines.Sum(line => line.LineTotal);
        var shipping = CalculateShipping(lines);
        var tax = CalculateTax(subtotal, shipping.Amount, shippingAddress);

        return new CheckoutPricingResult
        {
            CurrencyCode = cart.CurrencyCode,
            Subtotal = subtotal,
            ShippingAmount = shipping.Amount,
            ShippingMethod = shipping.Method,
            ShippingLabel = shipping.Label,
            TaxAmount = tax.Amount,
            TaxLabel = tax.Label,
            TaxRatePercent = tax.RatePercent,
            TotalAmount = subtotal + shipping.Amount + tax.Amount,
            Lines = lines
        };
    }

    private ShippingQuote CalculateShipping(IEnumerable<CheckoutPricingLine> lines)
    {
        var physicalLines = lines.Where(line => line.RequiresShipping).ToList();
        if (physicalLines.Count == 0)
        {
            return new ShippingQuote(0m, "DigitalDelivery", "Digital delivery");
        }

        var physicalSubtotal = physicalLines.Sum(line => line.LineTotal);
        if (options.FreeShippingThreshold > 0m && physicalSubtotal >= options.FreeShippingThreshold)
        {
            return new ShippingQuote(0m, "Standard", "Free standard shipping");
        }

        var physicalQuantity = physicalLines.Sum(line => line.CartItem.Quantity);
        var totalWeightKg = physicalLines.Sum(line => (line.WeightGrams * line.CartItem.Quantity) / 1000m);
        var extraItems = Math.Max(physicalQuantity - 1, 0);
        var amount =
            options.FlatPhysicalShippingRate +
            (extraItems * options.AdditionalPhysicalItemRate) +
            (totalWeightKg * options.PerKgShippingRate);

        return new ShippingQuote(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), "Standard", "Standard shipping");
    }

    private TaxQuote CalculateTax(decimal subtotal, decimal shippingAmount, ShippingAddressAggregate shippingAddress)
    {
        var domestic = string.Equals(shippingAddress.CountryCode, options.OriginCountryCode, StringComparison.OrdinalIgnoreCase);
        var intraState = domestic && string.Equals(shippingAddress.State, options.OriginState, StringComparison.OrdinalIgnoreCase);
        var rate = intraState ? options.IntraStateTaxRatePercent : options.InterStateTaxRatePercent;
        var taxableAmount = subtotal + (options.TaxShippingCharges ? shippingAmount : 0m);
        var taxAmount = decimal.Round(taxableAmount * rate / 100m, 2, MidpointRounding.AwayFromZero);
        return new TaxQuote(taxAmount, "GST", rate);
    }

    private readonly record struct ShippingQuote(decimal Amount, string Method, string Label);
    private readonly record struct TaxQuote(decimal Amount, string Label, decimal RatePercent);
}
