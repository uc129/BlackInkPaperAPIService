namespace Infrastructure.Services.Email;

/// <summary>
/// Copy for the account emails a customer receives, kept out of the controllers so the wording
/// lives in one place rather than being assembled inline next to the Identity calls.
///
/// Every template takes a fully-formed <c>actionLink</c>: composing the URL needs a public site
/// address, which is the caller's concern, not the copy's.
/// </summary>
public static class AccountEmailTemplates
{
    private const string BrandName = "Black Ink Paper";
    private const string SupportAddress = "hello@blackinkpaper.com";

    /// <summary>Sent by <c>POST /api/accounts/forgot-password</c>.</summary>
    public static (string Subject, string HtmlBody) PasswordReset(string actionLink, int expiryHours = 24) =>
    ($"Reset your {BrandName} password",
     Layout(
        heading: "Reset your password",
        bodyHtml: $"""
            <p style="margin:0 0 16px">We received a request to reset the password for your {BrandName} account.</p>
            <p style="margin:0 0 24px">Choose a new password using the button below. This link expires in {expiryHours} hours.</p>
            {Button(actionLink, "Reset my password")}
            <p style="margin:24px 0 0;font-size:14px;color:#6b6b6b">
              If you didn't ask to reset your password you can ignore this email — your password
              will stay as it is.
            </p>
            """,
        actionLink: actionLink));

    /// <summary>Sent by <c>POST /api/accounts/register</c>.</summary>
    public static (string Subject, string HtmlBody) ConfirmEmail(string actionLink, string? fullName) =>
    ($"Confirm your email — {BrandName}",
     Layout(
        heading: fullName is { Length: > 0 } name ? $"Welcome, {Escape(name)}" : "Welcome",
        bodyHtml: $"""
            <p style="margin:0 0 16px">
              Thanks for creating an account at {BrandName}, home to the original artwork and
              prints of Ria Mukharjee.
            </p>
            <p style="margin:0 0 24px">
              Confirm your email address to finish setting up your account. You'll need a
              confirmed email or phone number before you can place an order.
            </p>
            {Button(actionLink, "Confirm my email")}
            <p style="margin:24px 0 0;font-size:14px;color:#6b6b6b">
              If you didn't create this account, you can safely ignore this email.
            </p>
            """,
        actionLink: actionLink));

    /// <summary>
    /// Sent by <c>POST /api/accounts/email/link</c>, when a phone-first customer adds an email
    /// to an account they already have. Deliberately not the registration copy — they are not
    /// new here, and welcoming them again reads as a phishing attempt.
    /// </summary>
    public static (string Subject, string HtmlBody) ConfirmLinkedEmail(string actionLink) =>
    ($"Confirm your email address — {BrandName}",
     Layout(
        heading: "Confirm your email address",
        bodyHtml: $"""
            <p style="margin:0 0 16px">
              This email address was added to your {BrandName} account. Confirming it lets you
              sign in with email as well as your phone number, and is where we'll send your
              order receipts.
            </p>
            {Button(actionLink, "Confirm this address")}
            <p style="margin:24px 0 0;font-size:14px;color:#6b6b6b">
              If you didn't add this address to an account, please let us know at {SupportAddress}.
            </p>
            """,
        actionLink: actionLink));

    /// <summary>
    /// A single-column table layout with inline styles — the only combination Gmail, Outlook and
    /// Apple Mail all render consistently. Stylesheets and flexbox are unreliable in email.
    /// </summary>
    private static string Layout(string heading, string bodyHtml, string actionLink) => $"""
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#faf9f7;padding:32px 0">
          <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#ffffff;border:1px solid #e8e5e0;border-radius:8px">
              <tr><td style="padding:32px 32px 0">
                <div style="font:600 18px/1.3 Georgia,serif;color:#1a1a1a">{BrandName}</div>
              </td></tr>
              <tr><td style="padding:24px 32px 32px;font:400 16px/1.6 -apple-system,Segoe UI,Helvetica,Arial,sans-serif;color:#1a1a1a">
                <h1 style="margin:0 0 16px;font:600 22px/1.3 Georgia,serif">{heading}</h1>
                {bodyHtml}
              </td></tr>
              <tr><td style="padding:0 32px 32px;font:400 13px/1.6 -apple-system,Segoe UI,Helvetica,Arial,sans-serif;color:#8a8a8a;border-top:1px solid #f0ede8">
                <p style="margin:16px 0 8px">
                  If the button doesn't work, copy this link into your browser:<br>
                  <span style="word-break:break-all;color:#6b6b6b">{Escape(actionLink)}</span>
                </p>
                <p style="margin:8px 0 0">Questions? Reply to this email or write to {SupportAddress}.</p>
              </td></tr>
            </table>
          </td></tr>
        </table>
        """;

    private static string Button(string href, string label) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0"><tr>
          <td style="background:#1a1a1a;border-radius:6px">
            <a href="{Escape(href)}" style="display:inline-block;padding:12px 24px;font:600 15px/1 -apple-system,Segoe UI,Helvetica,Arial,sans-serif;color:#ffffff;text-decoration:none">{label}</a>
          </td>
        </tr></table>
        """;

    /// <summary>
    /// The reset link carries a user-supplied email and an Identity token in its query string,
    /// and the name comes straight from registration — all three land inside markup.
    /// </summary>
    private static string Escape(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
