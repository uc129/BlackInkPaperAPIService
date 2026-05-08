using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;


namespace Infrastructure.Services;

public sealed class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public sealed class SendGridEmailService(
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly SendGridOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var client = new SendGridClient(_options.ApiKey);
        var msg = MailHelper.CreateSingleEmail(
            from: new EmailAddress(_options.FromEmail, _options.FromName),
            to: new EmailAddress(to),
            subject: subject,
            plainTextContent: null,
            htmlContent: htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            logger.LogError("SendGrid delivery failed. Status={Status} Body={Body}", response.StatusCode, body);
        }
    }
}
