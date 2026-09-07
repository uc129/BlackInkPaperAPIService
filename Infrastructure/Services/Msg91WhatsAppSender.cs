using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Sends approved WhatsApp templates through MSG91.
///
/// !! UNVERIFIED WIRE FORMAT !! The endpoint path and body below were written without access
/// to MSG91's API reference, which is behind their dashboard. Check both against the current
/// docs before the first real send — this is a pre-launch task alongside the Meta template
/// approvals, not something to discover in production. Everything outside BuildPayload and
/// Endpoint is provider-shaped but format-independent, so a correction should be contained.
/// Until Msg91:AuthKey is configured, LoggingWhatsAppSender runs instead and this class is
/// never constructed.
/// </summary>
public sealed class Msg91WhatsAppSender(
    HttpClient httpClient,
    IOptions<Msg91Options> optionsAccessor,
    ILogger<Msg91WhatsAppSender> logger) : IWhatsAppSender
{
    private const string Endpoint = "/api/v5/whatsapp/whatsapp-outbound-message/bulk/";
    private const string LanguageCode = "en";

    private readonly Msg91Options options = optionsAccessor.Value;

    public async Task<MessageSendResult> SendTemplateAsync(
        string phoneE164,
        string templateName,
        IReadOnlyList<string> parameters,
        CancellationToken ct = default)
    {
        try
        {
            var payload = BuildPayload(phoneE164, templateName, parameters);

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("authkey", options.AuthKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                // Never log the recipient's number or the message parameters — an OTP would
                // end up in the log. The template name is enough to identify the send.
                logger.LogError(
                    "MSG91 WhatsApp send failed. Template={Template} Status={Status} Body={Body}",
                    templateName, response.StatusCode, body);
                return MessageSendResult.Fail($"WhatsApp send failed with status {(int)response.StatusCode}.");
            }

            return MessageSendResult.Ok(ExtractMessageId(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "MSG91 WhatsApp send threw. Template={Template}", templateName);
            return MessageSendResult.Fail(ex.Message);
        }
    }

    private object BuildPayload(string phoneE164, string templateName, IReadOnlyList<string> parameters) => new
    {
        integrated_number = options.WhatsAppNumber,
        content_type = "template",
        payload = new
        {
            messaging_product = "whatsapp",
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = LanguageCode, policy = "deterministic" },
                to_and_components = new[]
                {
                    new
                    {
                        to = new[] { phoneE164.TrimStart('+') },
                        components = parameters
                            .Select((value, index) => (Key: (index + 1).ToString(), Value: value))
                            .ToDictionary(p => p.Key, p => (object)new { type = "text", value = p.Value })
                    }
                }
            }
        }
    };

    private static string? ExtractMessageId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("request_id", out var id) ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
