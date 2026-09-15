using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Stand-ins used whenever Msg91:AuthKey is unset, mirroring how StubEmailService covers for
/// SendGrid. They let the whole phone-login and notification flow run locally with no vendor
/// account and no real messages.
///
/// These deliberately log the message parameters — including the OTP — because reading the
/// code out of the console is how you complete a login in development. That is also exactly
/// why they must never be selected in production: Program.cs picks them only when the auth
/// key is absent, and the deployed environment always has one.
/// </summary>
public sealed class LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger) : IWhatsAppSender
{
    public Task<MessageSendResult> SendTemplateAsync(
        string phoneE164,
        string templateName,
        IReadOnlyList<string> parameters,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[StubWhatsApp] To={To} Template={Template} Params={Params}",
            phoneE164, templateName, string.Join(" | ", parameters));

        return Task.FromResult(MessageSendResult.Ok($"stub-{Guid.NewGuid():N}"));
    }
}

public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task<MessageSendResult> SendAsync(
        string phoneE164,
        string dltTemplateId,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[StubSms] To={To} Template={Template} Params={Params}",
            phoneE164, dltTemplateId, string.Join(" | ", parameters.Select(p => $"{p.Key}={p.Value}")));

        return Task.FromResult(MessageSendResult.Ok($"stub-{Guid.NewGuid():N}"));
    }
}
