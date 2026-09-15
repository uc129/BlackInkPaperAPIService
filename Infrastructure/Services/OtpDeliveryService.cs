using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Delivers login codes over WhatsApp, falling back to SMS.
///
/// WhatsApp goes first because it is cheaper and lands more reliably in this market, but it
/// is not universal — a number with no WhatsApp account, or one logged out on that handset,
/// gets nothing and reports no error the sender can see. SMS is the floor that keeps such a
/// customer able to log in at all.
/// </summary>
public sealed class OtpDeliveryService(
    IWhatsAppSender whatsAppSender,
    ISmsSender smsSender,
    IOptions<Msg91Options> optionsAccessor,
    ILogger<OtpDeliveryService> logger) : IOtpDeliveryService
{
    /// <summary>
    /// The variable name the code is substituted into. It must match the variable registered
    /// on the DLT template referenced by Msg91:Templates:OtpSms, or the operator rejects the
    /// send.
    /// </summary>
    private const string SmsOtpVariable = "otp";

    private readonly Msg91Options options = optionsAccessor.Value;

    public async Task<OtpDeliveryResult> SendAsync(string phoneE164, string code, CancellationToken ct = default)
    {
        var whatsApp = await whatsAppSender.SendTemplateAsync(
            phoneE164, options.Templates.OtpWhatsApp, [code], ct);

        if (whatsApp.Success)
            return OtpDeliveryResult.Ok(OtpChannel.WhatsApp);

        logger.LogWarning("WhatsApp OTP failed, falling back to SMS. Error={Error}", whatsApp.Error);

        var sms = await smsSender.SendAsync(
            phoneE164,
            options.Templates.OtpSms,
            new Dictionary<string, string> { [SmsOtpVariable] = code },
            ct);

        if (sms.Success)
            return OtpDeliveryResult.Ok(OtpChannel.Sms);

        logger.LogError(
            "Both OTP channels failed. WhatsApp={WhatsAppError} Sms={SmsError}",
            whatsApp.Error, sms.Error);

        return OtpDeliveryResult.Fail("We could not send your code. Please try again shortly.");
    }
}
