using Domain.Entities;

namespace Infrastructure.Contracts.Repositories;

public interface INotificationOutboxRepository
{
    /// <summary>
    /// Adds a message unless one with the same dedupe key is already queued. Returns false
    /// when it was a duplicate, which callers may log but must not treat as an error — a
    /// captured payment legitimately arrives twice.
    /// </summary>
    Task<bool> EnqueueAsync(NotificationDelivery delivery, CancellationToken ct = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due messages, marking them
    /// Sending so a second dispatcher cannot pick up the same rows.
    /// </summary>
    Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(
        int batchSize, DateTime now, CancellationToken ct = default);

    Task MarkSentAsync(long id, string? providerMessageId, DateTime sentAt, CancellationToken ct = default);

    /// <summary>
    /// Returns a message to the queue for a later retry, or gives up on it once it has used
    /// all its attempts.
    /// </summary>
    Task MarkAttemptFailedAsync(
        long id, string error, DateTime retryAt, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Abandons a message outright, for failures no retry can fix — a payload that cannot be
    /// parsed will not parse on the fifth attempt either.
    /// </summary>
    Task MarkPermanentlyFailedAsync(long id, string error, DateTime now, CancellationToken ct = default);
}
