using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Accounts;

public class OtpCodeHasherTests
{
    private const string Phone = "+919876543210";
    private const string OtherPhone = "+919000000000";

    private static OtpCodeHasher CreateHasher(string key = "test-hash-key", int codeLength = 6)
        => new(Options.Create(new OtpOptions { HashKey = key, CodeLength = codeLength }));

    [Fact]
    public void GenerateCode_ReturnsDigitsOfConfiguredLength()
    {
        var hasher = CreateHasher();

        var code = hasher.GenerateCode();

        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
    }

    /// <summary>
    /// Short codes must keep their leading zeros — "004321" formatted as "4321" would be
    /// rejected when the customer typed exactly what they were sent.
    /// </summary>
    [Fact]
    public void GenerateCode_PadsWithLeadingZeros_WhenValueIsSmall()
    {
        var hasher = CreateHasher();

        for (var i = 0; i < 500; i++)
            Assert.Equal(6, hasher.GenerateCode().Length);
    }

    [Fact]
    public void Verify_ReturnsTrue_WhenCodeMatches()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash(Phone, "123456");

        Assert.True(hasher.Verify(Phone, "123456", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenCodeDiffers()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash(Phone, "123456");

        Assert.False(hasher.Verify(Phone, "123457", hash));
    }

    /// <summary>
    /// The phone number is mixed into the hash, so a hash observed for one number cannot be
    /// redeemed against another.
    /// </summary>
    [Fact]
    public void Verify_ReturnsFalse_WhenPhoneNumberDiffers()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash(Phone, "123456");

        Assert.False(hasher.Verify(OtherPhone, "123456", hash));
    }

    /// <summary>
    /// Without the server-side key the hash is just a digest of a six-digit number, which is
    /// exhaustible in milliseconds. A different key must produce a different hash.
    /// </summary>
    [Fact]
    public void Hash_DiffersAcrossKeys_ForTheSameCode()
    {
        var first = CreateHasher("key-one").Hash(Phone, "123456");
        var second = CreateHasher("key-two").Hash(Phone, "123456");

        Assert.NotEqual(first, second);
    }
}
