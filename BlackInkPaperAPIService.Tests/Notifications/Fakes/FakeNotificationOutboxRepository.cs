using Domain.Entities;
using Infrastructure.Contracts.Repositories;

namespace BlackInkPaperAPIService.Tests.Notifications.Fakes;

internal sealed class FakeNotificationOutboxRepository : INotificationOutboxRepository
{
    // ── Configurable handlers ────────────────────────────────────────────────
    public Func<NotificationDelivery, Task<bool>> EnqueueHandler { get; set; }
        = _ => Task.FromResult(true);

    // ── Captured values ──────────────────────────────────────────────────────
    public List<NotificationDelivery> Enqueued { get; } = [];

    public Task<bool> EnqueueAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        Enqueued.Add(delivery);
        return EnqueueHandler(delivery);
    }

    public Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(
        int batchSize, DateTime now, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NotificationDelivery>>([]);

    public Task MarkSentAsync(long id, string? providerMessageId, DateTime sentAt, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task MarkAttemptFailedAsync(
        long id, string error, DateTime retryAt, DateTime now, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task MarkPermanentlyFailedAsync(long id, string error, DateTime now, CancellationToken ct = default)
        => Task.CompletedTask;
}
