using PhoneNumbers;

namespace Infrastructure.Services;

public readonly record struct PhoneNormalizationResult(bool Success, string Value, string? Error)
{
    public static PhoneNormalizationResult Ok(string e164) => new(true, e164, null);
    public static PhoneNormalizationResult Fail(string error) => new(false, string.Empty, error);
}

/// <summary>
/// Collapses the ways an Indian customer might type their number into one canonical E.164
/// string. This has to be exact: the normalized value is the account's login handle, so two
/// spellings that fail to converge become two accounts for one person, each with its own cart
/// and order history.
/// </summary>
public static class PhoneNumberNormalizer
{
    private const string DefaultRegion = "IN";
    private const string IndiaCountryCode = "91";

    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    public static PhoneNormalizationResult Normalize(string? input, string defaultRegion = DefaultRegion)
    {
        if (string.IsNullOrWhiteSpace(input))
            return PhoneNormalizationResult.Fail("Phone number is required.");

        var candidate = Prepare(input, defaultRegion);

        try
        {
            var parsed = Util.Parse(candidate, defaultRegion);

            if (!Util.IsValidNumber(parsed))
                return PhoneNormalizationResult.Fail("That does not look like a valid phone number.");

            // OTP and WhatsApp both need a handset. Landlines parse as valid but can never
            // receive either, so rejecting here beats a message that silently never arrives.
            var numberType = Util.GetNumberType(parsed);
            if (numberType is not (PhoneNumberType.MOBILE or PhoneNumberType.FIXED_LINE_OR_MOBILE))
                return PhoneNormalizationResult.Fail("Enter a mobile number — we send a code by WhatsApp or SMS.");

            return PhoneNormalizationResult.Ok(Util.Format(parsed, PhoneNumberFormat.E164));
        }
        catch (NumberParseException)
        {
            return PhoneNormalizationResult.Fail("That does not look like a valid phone number.");
        }
    }

    /// <summary>
    /// Handles the one shape libphonenumber cannot read on its own: a country code with no
    /// '+' in front of it. "919876543210" parsed against region IN is a 12-digit national
    /// number and invalid, but the user plainly meant +91 98765 43210.
    /// </summary>
    private static string Prepare(string input, string defaultRegion)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith('+'))
            return trimmed;

        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());

        if (defaultRegion == DefaultRegion
            && digits.Length == 12
            && digits.StartsWith(IndiaCountryCode, StringComparison.Ordinal))
        {
            return "+" + digits;
        }

        return trimmed;
    }
}
