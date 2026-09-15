namespace Domain.Entities;

public sealed class NotificationDelivery
{
    public long Id { get; set; }

    /// <summary>
    /// Uniquely identifies the thing being announced, e.g. "Confirmed:1234". The unique
    /// constraint on this column is what stops a customer being told twice that the same
    /// order was confirmed when both the client and the payment webhook report the capture.
    /// </summary>
    public string DedupeKey { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>JSON: the template parameters, in the order the template expects them.</summary>
    public string Payload { get; set; } = "{}";

    public string Status { get; set; } = NotificationDeliveryStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public string? LastError { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public static class NotificationDeliveryStatus
{
    public const string Pending = "Pending";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}
