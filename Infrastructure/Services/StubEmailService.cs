using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

// Logs emails to the console; swap for a real SMTP/SendGrid implementation later.
public class StubEmailService(ILogger<StubEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("[StubEmail] To={To} Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
