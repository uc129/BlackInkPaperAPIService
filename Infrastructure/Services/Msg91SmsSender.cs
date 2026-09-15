using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Sends DLT-registered transactional SMS through MSG91's Flow API.
///
/// !! UNVERIFIED WIRE FORMAT !! Same caveat as Msg91WhatsAppSender — confirm Endpoint and
/// BuildPayload against MSG91's API reference before the first real send.
///
/// Nothing sends until the TRAI DLT registration completes: the entity, the sender header in
/// Msg91:SmsSenderId, and the OTP template whose id goes in Msg91:Templates:OtpSms all have
/// to be approved on the DLT portal first. A send before then is rejected by the operator,
/// not by this code.
/// </summary>
public sealed class Msg91SmsSender(
    HttpClient httpClient,
    IOptions<Msg91Options> optionsAccessor,
    ILogger<Msg91SmsSender> logger) : ISmsSender
{
    private const string Endpoint = "/api/v5/flow/";

    private readonly Msg91Options options = optionsAccessor.Value;

    public async Task<MessageSendResult> SendAsync(
        string phoneE164,
        string dltTemplateId,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dltTemplateId))
            return MessageSendResult.Fail("No DLT template id is configured for this message.");

        try
        {
            var recipient = new Dictionary<string, object> { ["mobiles"] = phoneE164.TrimStart('+') };
            foreach (var (key, value) in parameters)
                recipient[key] = value;

            var payload = new
            {
                template_id = dltTemplateId,
                sender = options.SmsSenderId,
                short_url = "0",
                recipients = new[] { recipient }
            };

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
                logger.LogError(
                    "MSG91 SMS send failed. Template={Template} Status={Status} Body={Body}",
                    dltTemplateId, response.StatusCode, body);
                return MessageSendResult.Fail($"SMS send failed with status {(int)response.StatusCode}.");
            }

            return MessageSendResult.Ok();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "MSG91 SMS send threw. Template={Template}", dltTemplateId);
            return MessageSendResult.Fail(ex.Message);
        }
    }
}
