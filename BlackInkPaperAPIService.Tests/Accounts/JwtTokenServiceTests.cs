using System.IdentityModel.Tokens.Jwt;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Accounts;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "a-test-signing-key-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        return new JwtTokenService(config);
    }

    /// <summary>
    /// A phone-first account has no email at all. Claim's constructor throws on a null value,
    /// so adding the email claim unconditionally made token issuance — and therefore every
    /// phone login — fail at the last step.
    /// </summary>
    [Fact]
    public void GenerateToken_Succeeds_WhenUserHasNoEmail()
    {
        var user = new AppIdentityUser
        {
            Id = "user-1",
            UserName = "+919876543210",
            PhoneNumber = "+919876543210",
            Email = null,
            FullName = "Test Customer"
        };

        var token = CreateService().GenerateToken(user, []);

        Assert.False(string.IsNullOrWhiteSpace(token));

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.DoesNotContain(parsed.Claims, c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.Contains(parsed.Claims, c => c.Value == "+919876543210");
    }

    [Fact]
    public void GenerateToken_IncludesEmailClaim_WhenUserHasAnEmail()
    {
        var user = new AppIdentityUser
        {
            Id = "user-2",
            UserName = "artist@example.com",
            Email = "artist@example.com",
            FullName = "Email Customer"
        };

        var token = CreateService().GenerateToken(user, ["User"]);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(parsed.Claims, c => c.Value == "artist@example.com");
    }

    [Fact]
    public void GenerateToken_Succeeds_WhenUserHasNeitherEmailNorPhone()
    {
        var user = new AppIdentityUser { Id = "user-3", UserName = "legacy", FullName = "Legacy" };

        var token = CreateService().GenerateToken(user, []);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}
