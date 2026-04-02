namespace Infrastructure.Contracts.Services;

public interface IRazorpayGateway
{
    string KeyId { get; }
    Task<RazorpayOrderResult> CreateOrderAsync(long amountInSubunits, string currency, string receipt, Dictionary<string, string>? notes, CancellationToken cancellationToken = default);
    Task<RazorpayPaymentResult?> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);
    bool VerifyWebhookSignature(string rawBody, string signature);
}

public sealed class RazorpayOrderResult
{
    public string Id { get; init; } = string.Empty;
    public string Currency { get; init; } = "INR";
    public long Amount { get; init; }
    public string Receipt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class RazorpayPaymentResult
{
    public string Id { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public long Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Status { get; init; } = string.Empty;
    public string? OrderId { get; init; }
    public string? Method { get; init; }
}
