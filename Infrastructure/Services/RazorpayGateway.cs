using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class RazorpayGateway(
    HttpClient httpClient,
    IOptions<RazorpayOptions> optionsAccessor) : IRazorpayGateway
{
    private readonly RazorpayOptions options = optionsAccessor.Value;

    public string KeyId => options.KeyId;

    public async Task<RazorpayOrderResult> CreateOrderAsync(long amountInSubunits, string currency, string receipt, Dictionary<string, string>? notes, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            amount = amountInSubunits,
            currency,
            receipt,
            notes
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await SendAsync(request, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new RazorpayOrderResult
        {
            Id = root.GetProperty("id").GetString() ?? string.Empty,
            Currency = root.GetProperty("currency").GetString() ?? currency,
            Amount = root.GetProperty("amount").GetInt64(),
            Receipt = root.TryGetProperty("receipt", out var receiptValue) ? receiptValue.GetString() ?? receipt : receipt,
            Status = root.TryGetProperty("status", out var statusValue) ? statusValue.GetString() ?? string.Empty : string.Empty
        };
    }

    public async Task<RazorpayPaymentResult?> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/payments/{paymentId}");
        using var response = await SendAsync(request, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new RazorpayPaymentResult
        {
            Id = root.GetProperty("id").GetString() ?? string.Empty,
            Entity = root.TryGetProperty("entity", out var entityValue) ? entityValue.GetString() ?? string.Empty : string.Empty,
            Amount = root.GetProperty("amount").GetInt64(),
            Currency = root.GetProperty("currency").GetString() ?? "INR",
            Status = root.TryGetProperty("status", out var statusValue) ? statusValue.GetString() ?? string.Empty : string.Empty,
            OrderId = root.TryGetProperty("order_id", out var orderIdValue) ? orderIdValue.GetString() : null,
            Method = root.TryGetProperty("method", out var methodValue) ? methodValue.GetString() : null
        };
    }

    public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        => FixedTimeEquals(ComputeHex($"{razorpayOrderId}|{razorpayPaymentId}", options.KeySecret), razorpaySignature);

    public bool VerifyWebhookSignature(string rawBody, string signature)
        => FixedTimeEquals(ComputeHex(rawBody, options.WebhookSecret), signature);

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.KeyId}:{options.KeySecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Razorpay request failed with status {(int)response.StatusCode}: {body}");
    }

    private static string ComputeHex(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
