namespace Infrastructure.Contracts.Services;

/// <summary>
/// Business-initiated WhatsApp messages must use a template approved by Meta in advance;
/// there is no free-text send outside a live customer service window. Hence template name
/// plus positional parameters rather than a message body.
/// </summary>
public interface IWhatsAppSender
{
    Task<MessageSendResult> SendTemplateAsync(
        string phoneE164,
        string templateName,
        IReadOnlyList<string> parameters,
        CancellationToken ct = default);
}

/// <summary>
/// Indian SMS is likewise not free-text: the body must correspond to a template registered
/// with TRAI on the DLT portal, referenced here by its DLT template id.
/// </summary>
public interface ISmsSender
{
    Task<MessageSendResult> SendAsync(
        string phoneE164,
        string dltTemplateId,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default);
}

public sealed record MessageSendResult(bool Success, string? ProviderMessageId, string? Error)
{
    public static MessageSendResult Ok(string? providerMessageId = null) => new(true, providerMessageId, null);
    public static MessageSendResult Fail(string error) => new(false, null, error);
}
