namespace Infrastructure.Configuration;

public class Msg91Options
{
    public const string SectionName = "Msg91";

    public string BaseUrl { get; set; } = "https://control.msg91.com";

    /// <summary>
    /// Supplied out of band as the environment variable Msg91__AuthKey (an Azure App Setting
    /// in production), never from appsettings.json — the same rule as SendGrid__ApiKey. When
    /// it is blank the logging senders are registered instead, so local development neither
    /// needs an account nor sends anything real.
    /// </summary>
    public string AuthKey { get; set; } = string.Empty;

    /// <summary>The WABA sender number, in E.164 without the leading '+'.</summary>
    public string WhatsAppNumber { get; set; } = string.Empty;

    /// <summary>The 6-character DLT-registered SMS header. Nothing sends until DLT clears.</summary>
    public string SmsSenderId { get; set; } = string.Empty;

    public Msg91TemplateOptions Templates { get; set; } = new();
}

/// <summary>
/// Provider-side template names. WhatsApp templates must be approved by Meta before they can
/// be sent, and the SMS OTP template must additionally be registered on the TRAI DLT portal.
/// </summary>
public class Msg91TemplateOptions
{
    public string OtpWhatsApp { get; set; } = "otp_login";

    /// <summary>DLT template id, not a name — the SMS OTP body is registered with TRAI.</summary>
    public string OtpSms { get; set; } = string.Empty;

    public string OrderConfirmed { get; set; } = "order_confirmed";
    public string OrderShipped { get; set; } = "order_shipped";
    public string OrderDelivered { get; set; } = "order_delivered";
    public string OrderCancelled { get; set; } = "order_cancelled";
    public string PaymentFailed { get; set; } = "payment_failed";
}
