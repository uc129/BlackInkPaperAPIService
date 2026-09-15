namespace Infrastructure.Contracts.Services;

public enum OtpChannel
{
    WhatsApp,
    Sms
}

public interface IOtpDeliveryService
{
    /// <summary>
    /// Delivers a login code, preferring WhatsApp and falling back to SMS. Returns the channel
    /// that actually accepted it so the caller can tell the customer where to look.
    /// </summary>
    Task<OtpDeliveryResult> SendAsync(string phoneE164, string code, CancellationToken ct = default);
}

public sealed record OtpDeliveryResult(bool Success, OtpChannel Channel, string? Error)
{
    public static OtpDeliveryResult Ok(OtpChannel channel) => new(true, channel, null);
    public static OtpDeliveryResult Fail(string error) => new(false, OtpChannel.WhatsApp, error);
}
