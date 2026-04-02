namespace Infrastructure.Configuration;

public class CheckoutPricingOptions
{
    public string OriginCountryCode { get; set; } = "IN";
    public string OriginState { get; set; } = string.Empty;
    public decimal IntraStateTaxRatePercent { get; set; } = 18m;
    public decimal InterStateTaxRatePercent { get; set; } = 18m;
    public bool TaxShippingCharges { get; set; } = true;
    public decimal FreeShippingThreshold { get; set; } = 1500m;
    public decimal FlatPhysicalShippingRate { get; set; } = 99m;
    public decimal AdditionalPhysicalItemRate { get; set; } = 25m;
    public decimal PerKgShippingRate { get; set; } = 40m;
}
