using Dapper;
using Domain.Entities;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class NotificationOutboxRepository(IDapperContext dapperContext) : INotificationOutboxRepository
{
    /// <summary>
    /// How long a claimed row may sit in 'Sending' before another dispatcher may take it over.
    /// Comfortably longer than a send attempt, short enough that a redeploy does not delay a
    /// customer's order confirmation by much.
    /// </summary>
    private const int StaleClaimMinutes = 5;

    public async Task<bool> EnqueueAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        // ON CONFLICT DO NOTHING is the whole deduplication mechanism: the second caller for
        // the same event inserts nothing and is told so by the affected-row count.
        const string sql = """
            INSERT INTO notification_deliveries
                (dedupe_key, user_id, recipient, channel, template_name, payload,
                 status, attempts, max_attempts, scheduled_for, created_at, updated_at)
            VALUES
                (@DedupeKey, @UserId, @Recipient, @Channel, @TemplateName, @Payload::jsonb,
                 @Status, @Attempts, @MaxAttempts, @ScheduledFor, @CreatedAt, @UpdatedAt)
            ON CONFLICT (dedupe_key) DO NOTHING;
            """;

        using var connection = dapperContext.CreateConnection();
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, delivery, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(
        int batchSize, DateTime now, CancellationToken ct = default)
    {
        // FOR UPDATE SKIP LOCKED lets several instances drain the queue at once without any
        // of them handing out the same row twice.
        //
        // Rows left in 'Sending' are reclaimed after StaleClaimMinutes. Without that, a
        // process that dies or is redeployed mid-batch would strand its claimed rows in a
        // status nothing ever selects again — losing exactly the order confirmations the
        // outbox exists to guarantee.
        const string sql = """
            UPDATE notification_deliveries
            SET status = 'Sending', updated_at = @Now
            WHERE id IN (
                SELECT id FROM notification_deliveries
                WHERE scheduled_for <= @Now
                  AND (status = 'Pending' OR (status = 'Sending' AND updated_at < @StaleBefore))
                ORDER BY scheduled_for
                LIMIT @BatchSize
                FOR UPDATE SKIP LOCKED
            )
            RETURNING *;
            """;

        using var connection = dapperContext.CreateConnection();
        var claimed = await connection.QueryAsync<NotificationDelivery>(
            new CommandDefinition(
                sql,
                new { Now = now, BatchSize = batchSize, StaleBefore = now.AddMinutes(-StaleClaimMinutes) },
                cancellationToken: ct));

        return claimed.ToList();
    }

    public async Task MarkSentAsync(long id, string? providerMessageId, DateTime sentAt, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notification_deliveries
            SET status = 'Sent', sent_at = @SentAt, updated_at = @SentAt,
                attempts = attempts + 1, provider_message_id = @ProviderMessageId
            WHERE id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, ProviderMessageId = providerMessageId, SentAt = sentAt }, cancellationToken: ct));
    }

    public async Task MarkAttemptFailedAsync(
        long id, string error, DateTime retryAt, DateTime now, CancellationToken ct = default)
    {
        // A row goes back to Pending for another try, or to Failed once it has spent its
        // attempts — decided in SQL so a crash between reading and writing cannot lose it.
        const string sql = """
            UPDATE notification_deliveries
            SET attempts = attempts + 1,
                last_error = @Error,
                updated_at = @Now,
                status = CASE WHEN attempts + 1 >= max_attempts THEN 'Failed' ELSE 'Pending' END,
                scheduled_for = CASE WHEN attempts + 1 >= max_attempts THEN scheduled_for ELSE @RetryAt END
            WHERE id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Error = error, RetryAt = retryAt, Now = now }, cancellationToken: ct));
    }

    public async Task MarkPermanentlyFailedAsync(long id, string error, DateTime now, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notification_deliveries
            SET status = 'Failed', last_error = @Error, updated_at = @Now, attempts = attempts + 1
            WHERE id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Error = error, Now = now }, cancellationToken: ct));
    }
}
