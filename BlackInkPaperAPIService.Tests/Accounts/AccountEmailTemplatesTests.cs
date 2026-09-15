using Infrastructure.Services.Email;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Accounts;

public class AccountEmailTemplatesTests
{
    private const string Link = "https://blackinkpaper.com/reset-password?email=a%40b.com&token=abc123";

    [Fact]
    public void PasswordReset_IncludesActionLink_InButtonAndFallback()
    {
        var (subject, body) = AccountEmailTemplates.PasswordReset(Link);

        // Ampersands are HTML-encoded, which is correct inside an href — the browser decodes
        // them back. Asserting the raw string here would be asserting a bug.
        var encoded = System.Net.WebUtility.HtmlEncode(Link);

        Assert.Contains("Reset your", subject);
        Assert.Contains($"href=\"{encoded}\"", body);
        // The plain-text fallback matters: many clients strip or mangle the button.
        Assert.Contains(encoded, body);
        Assert.Contains("24 hours", body);
    }

    [Fact]
    public void PasswordReset_TellsRecipientTheyCanIgnoreIt_WhenTheyDidNotRequestIt()
    {
        var (_, body) = AccountEmailTemplates.PasswordReset(Link);

        Assert.Contains("didn't ask to reset your password", body);
    }

    [Fact]
    public void ConfirmEmail_GreetsByName_WhenNameSupplied()
    {
        var (_, body) = AccountEmailTemplates.ConfirmEmail(Link, "Ria Mukharjee");

        Assert.Contains("Welcome, Ria Mukharjee", body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ConfirmEmail_FallsBackToPlainWelcome_WhenNameMissing(string? fullName)
    {
        var (_, body) = AccountEmailTemplates.ConfirmEmail(Link, fullName);

        Assert.Contains("Welcome", body);
        Assert.DoesNotContain("Welcome, ", body);
    }

    [Fact]
    public void ConfirmEmail_EscapesName_SoMarkupCannotBeInjected()
    {
        // FullName is free text taken at registration and rendered into the heading.
        var (_, body) = AccountEmailTemplates.ConfirmEmail(Link, "<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", body);
        Assert.Contains("&lt;script&gt;", body);
    }

    [Fact]
    public void AllTemplates_EscapeTheActionLink_SoQueryStringCannotBreakOutOfTheAttribute()
    {
        const string hostile = "https://x.test/reset?token=a\"><script>alert(1)</script>";

        foreach (var (_, body) in new[]
                 {
                     AccountEmailTemplates.PasswordReset(hostile),
                     AccountEmailTemplates.ConfirmEmail(hostile, "Test"),
                     AccountEmailTemplates.ConfirmLinkedEmail(hostile),
                 })
        {
            Assert.DoesNotContain("<script>", body);
        }
    }

    [Fact]
    public void ConfirmLinkedEmail_DoesNotWelcomeAnExistingCustomer()
    {
        var (subject, body) = AccountEmailTemplates.ConfirmLinkedEmail(Link);

        Assert.Contains("Confirm your email address", subject);
        Assert.DoesNotContain("Welcome", body);
    }
}

public class AccountLinkBuilderTests
{
    private static AccountLinkBuilder Builder(string baseUrl) =>
        new(Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Configuration.FrontendOptions { BaseUrl = baseUrl }));

    [Theory]
    [InlineData("https://blackinkpaper-store.vercel.app")]
    [InlineData("https://blackinkpaper-store.vercel.app/")]   // trailing slash must not double up
    public void ResetPassword_BuildsAbsoluteUrl_RegardlessOfTrailingSlash(string baseUrl)
    {
        var link = Builder(baseUrl).ResetPassword("a@b.com", "tok123");

        Assert.Equal(
            "https://blackinkpaper-store.vercel.app/reset-password?email=a%40b.com&token=tok123",
            link);
    }

    [Fact]
    public void ConfirmEmail_EscapesEmailAndToken_SoQueryStringSurvivesRoundTrip()
    {
        // Identity tokens are base64-ish and routinely contain + and / characters.
        var link = Builder("https://x.test").ConfirmEmail("a+b@c.com", "CfDJ8A+b/c==");

        Assert.Contains("email=a%2Bb%40c.com", link);
        Assert.Contains("token=CfDJ8A%2Bb%2Fc%3D%3D", link);
    }

    [Fact]
    public void Build_FallsBackToRelativeLink_WhenBaseUrlNotConfigured()
    {
        var link = Builder("").ResetPassword("a@b.com", "tok");

        Assert.StartsWith("reset-password?", link);
    }
}
