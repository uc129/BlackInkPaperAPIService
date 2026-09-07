namespace Domain.Entities;

public sealed class PhoneVerificationCode
{
    public long Id { get; set; }
    public string PhoneE164 { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RequestIp { get; set; }
    public string? Channel { get; set; }

    /// <summary>
    /// A challenge is only worth checking a code against while it is unused, unexpired, and
    /// still has attempts left. Anything else must be treated as no live challenge at all.
    /// </summary>
    public bool IsRedeemable => ConsumedAt is null
                                && ExpiresAt > DateTime.UtcNow
                                && Attempts < MaxAttempts;
}
