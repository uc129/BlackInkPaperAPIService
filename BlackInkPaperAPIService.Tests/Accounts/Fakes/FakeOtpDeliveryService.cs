using Infrastructure.Contracts.Services;

namespace BlackInkPaperAPIService.Tests.Accounts.Fakes;

internal sealed class FakeOtpDeliveryService : IOtpDeliveryService
{
    public Func<string, string, Task<OtpDeliveryResult>> SendHandler { get; set; }
        = (_, _) => Task.FromResult(OtpDeliveryResult.Ok(OtpChannel.WhatsApp));

    public string? SentToPhone { get; private set; }
    public string? SentCode { get; private set; }
    public int SendCount { get; private set; }

    public Task<OtpDeliveryResult> SendAsync(string phoneE164, string code, CancellationToken ct = default)
    {
        SentToPhone = phoneE164;
        SentCode = code;
        SendCount++;
        return SendHandler(phoneE164, code);
    }
}
