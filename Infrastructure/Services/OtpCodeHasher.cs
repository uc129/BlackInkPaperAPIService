using System.Security.Cryptography;
using System.Text;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Generates and verifies one-time codes.
///
/// The stored value is an HMAC rather than a plain digest, because a six-digit code has only
/// a million possible values — every one of them could be enumerated against a leaked table
/// of SHA-256 hashes in well under a second. Keying the hash on a server-side secret means
/// the table alone is not enough. The phone number goes into the message as a salt, so a hash
/// captured for one number cannot be replayed against another.
/// </summary>
public sealed class OtpCodeHasher(IOptions<OtpOptions> optionsAccessor) : IOtpCodeHasher
{
    private readonly OtpOptions options = optionsAccessor.Value;

    public string GenerateCode()
    {
        var length = Math.Clamp(options.CodeLength, 4, 8);
        var max = (int)Math.Pow(10, length);

        // RandomNumberGenerator, not Random: a predictable OTP is not a one-time code.
        var value = RandomNumberGenerator.GetInt32(max);
        return value.ToString(new string('0', length));
    }

    public string Hash(string phoneE164, string code)
    {
        var key = Encoding.UTF8.GetBytes(options.HashKey);
        var message = Encoding.UTF8.GetBytes($"{phoneE164}:{code}");
        return Convert.ToHexString(HMACSHA256.HashData(key, message)).ToLowerInvariant();
    }

    public bool Verify(string phoneE164, string code, string expectedHash)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(phoneE164, code)),
            Encoding.UTF8.GetBytes(expectedHash));
}
