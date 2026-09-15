namespace Infrastructure.Configuration;

public class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>
    /// Secret the OTP HMAC is keyed on. Supplied out of band as the environment variable
    /// Otp__HashKey; Program.cs falls back to the JWT key so local development works without
    /// extra configuration. Rotating it invalidates every OTP still in flight, which is
    /// harmless — they expire in minutes.
    /// </summary>
    public string HashKey { get; set; } = string.Empty;

    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 10;
    public int MaxVerifyAttempts { get; set; } = 5;

    /// <summary>Sends allowed per phone number within <see cref="SendWindowMinutes"/>.</summary>
    public int MaxSendsPerWindow { get; set; } = 3;
    public int SendWindowMinutes { get; set; } = 10;
}
