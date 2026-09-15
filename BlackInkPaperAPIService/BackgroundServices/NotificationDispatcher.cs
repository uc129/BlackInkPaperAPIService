using System.Text.Json;
using Domain.Entities;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;

namespace BlackInkPaperAPIService.BackgroundServices;

/// <summary>
/// Drains the notification outbox.
///
/// It lives in the host rather than in Infrastructure because a background loop is a hosting
/// concern, and it takes a fresh DI scope per pass because the repositories and senders are
/// scoped while this service is a singleton.
///
/// Order notifications go out over WhatsApp only, so this resolves just IWhatsAppSender. The
/// delivery row carries a Channel column for when that stops being true; nothing reads it
/// yet. SMS is currently used only for the OTP fallback, which sends inline rather than
/// through this queue because the customer is waiting on it.
/// </summary>
public sealed class NotificationDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // The loop must outlive any single failure — a dead dispatcher means orders
                // silently stop being acknowledged.
                logger.LogError(ex, "Notification dispatch pass failed; continuing.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Notification dispatcher stopped.");
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<INotificationOutboxRepository>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();

        var due = await outbox.ClaimDueAsync(BatchSize, DateTime.UtcNow, ct);
        if (due.Count == 0)
            return;

        foreach (var delivery in due)
        {
            ct.ThrowIfCancellationRequested();
            await DispatchAsync(outbox, whatsApp, delivery, ct);
        }
    }

    private async Task DispatchAsync(
        INotificationOutboxRepository outbox,
        IWhatsAppSender whatsApp,
        NotificationDelivery delivery,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var parameters = ReadParameters(delivery.Payload);
        if (parameters is null)
        {
            // Abandoned rather than retried: a payload that will not parse now will not parse
            // in five minutes, and retrying it would just spin every poll until its attempts
            // ran out.
            await outbox.MarkPermanentlyFailedAsync(delivery.Id, "Payload could not be parsed.", now, ct);
            logger.LogError("Notification {Id} has an unreadable payload.", delivery.Id);
            return;
        }

        var result = await whatsApp.SendTemplateAsync(delivery.Recipient, delivery.TemplateName, parameters, ct);

        if (result.Success)
        {
            await outbox.MarkSentAsync(delivery.Id, result.ProviderMessageId, now, ct);
            return;
        }

        await outbox.MarkAttemptFailedAsync(
            delivery.Id, result.Error ?? "Unknown send failure.", NextRetry(now, delivery.Attempts), now, ct);

        logger.LogWarning(
            "Notification {Id} ({Template}) failed on attempt {Attempt}. Error={Error}",
            delivery.Id, delivery.TemplateName, delivery.Attempts + 1, result.Error);
    }

    /// <summary>Exponential backoff, capped so a retry is never more than an hour out.</summary>
    private static DateTime NextRetry(DateTime now, int attempts)
        => now.AddMinutes(Math.Min(Math.Pow(2, attempts), 60));

    private static IReadOnlyList<string>? ReadParameters(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("parameters", out var parameters))
                return null;

            return parameters.EnumerateArray()
                .Select(p => p.GetString() ?? string.Empty)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
