namespace Infrastructure.Configuration;

public class RazorpayOptions
{
    public string BaseUrl { get; set; } = "https://api.razorpay.com";
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Black Ink Paper";
    public string DisplayDescription { get; set; } = "Artwork purchase";
}
