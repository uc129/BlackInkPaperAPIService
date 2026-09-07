using Infrastructure.Services;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Accounts;

public class PhoneNumberNormalizerTests
{
    private const string Canonical = "+919876543210";

    /// <summary>
    /// The normalized number is the account's login handle, so every way a customer might
    /// type the same number has to collapse to one value. If any of these diverged, one
    /// person would end up with several accounts, each holding a different cart.
    /// </summary>
    [Theory]
    [InlineData("9876543210")]        // bare 10-digit
    [InlineData("09876543210")]       // national trunk prefix
    [InlineData("919876543210")]      // country code, no plus
    [InlineData("+919876543210")]     // already canonical
    [InlineData("+91 98765 43210")]   // spaced
    [InlineData("+91-98765-43210")]   // hyphenated
    [InlineData("  9876543210  ")]    // padded
    public void Normalize_ReturnsCanonicalE164_WhenGivenAnyIndianFormat(string input)
    {
        var result = PhoneNumberNormalizer.Normalize(input);

        Assert.True(result.Success, result.Error);
        Assert.Equal(Canonical, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_ReturnsFailure_WhenInputIsBlank(string? input)
    {
        var result = PhoneNumberNormalizer.Normalize(input);

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("00000000000000")]
    [InlineData("not a phone number")]
    public void Normalize_ReturnsFailure_WhenNumberIsNotValid(string input)
    {
        var result = PhoneNumberNormalizer.Normalize(input);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    /// <summary>
    /// A landline parses as a valid Indian number but can never receive an OTP, so it has to
    /// be refused at entry rather than accepted into a login that silently never completes.
    /// </summary>
    [Fact]
    public void Normalize_ReturnsFailure_WhenNumberIsALandline()
    {
        var result = PhoneNumberNormalizer.Normalize("+911123456789");

        Assert.False(result.Success);
        Assert.Contains("mobile", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Normalize_KeepsCountryCode_WhenNumberIsNotIndian()
    {
        var result = PhoneNumberNormalizer.Normalize("+14155552671");

        Assert.True(result.Success, result.Error);
        Assert.Equal("+14155552671", result.Value);
    }
}
