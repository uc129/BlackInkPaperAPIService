using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Email;

/// <summary>
/// Builds the absolute storefront links that account emails point at.
///
/// Kept apart from <see cref="AccountEmailTemplates"/> so copy and routing change independently:
/// the paths here must match pages the storefront actually hosts, which is a different concern
/// from what the email says.
/// </summary>
public sealed class AccountLinkBuilder(IOptions<FrontendOptions> options)
{
    private const string ResetPasswordPath = "reset-password";
    private const string ConfirmEmailPath = "confirm-email";

    private readonly FrontendOptions options = options.Value;

    public string ResetPassword(string email, string token) => Build(ResetPasswordPath, email, token);

    public string ConfirmEmail(string email, string token) => Build(ConfirmEmailPath, email, token);

    /// <summary>
    /// Falls back to a relative link when no base URL is configured. That link is useless in an
    /// email, but it keeps local development and tests working without the setting, and it fails
    /// visibly rather than throwing during a registration the customer would otherwise complete.
    /// </summary>
    private string Build(string path, string email, string token)
    {
        var query = $"{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        return string.IsNullOrWhiteSpace(options.BaseUrl)
            ? query
            : $"{options.BaseUrl.TrimEnd('/')}/{query}";
    }
}
