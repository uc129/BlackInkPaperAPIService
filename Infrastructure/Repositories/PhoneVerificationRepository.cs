using Dapper;
using Domain.Entities;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class PhoneVerificationRepository(IDapperContext dapperContext) : IPhoneVerificationRepository
{
    public async Task<int> CountSendsSinceAsync(string phoneE164, DateTime since, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) FROM phone_verification_codes
            WHERE phone_e164 = @PhoneE164 AND created_at >= @Since;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { PhoneE164 = phoneE164, Since = since }, cancellationToken: ct));
    }

    public async Task ExpireLiveChallengesAsync(string phoneE164, DateTime now, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE phone_verification_codes
            SET expires_at = @Now
            WHERE phone_e164 = @PhoneE164 AND consumed_at IS NULL AND expires_at > @Now;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { PhoneE164 = phoneE164, Now = now }, cancellationToken: ct));
    }

    public async Task<long> AddAsync(PhoneVerificationCode code, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO phone_verification_codes
                (phone_e164, code_hash, purpose, attempts, max_attempts,
                 expires_at, created_at, request_ip, channel)
            VALUES
                (@PhoneE164, @CodeHash, @Purpose, @Attempts, @MaxAttempts,
                 @ExpiresAt, @CreatedAt, @RequestIp, @Channel)
            RETURNING id;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, code, cancellationToken: ct));
    }

    public async Task<PhoneVerificationCode?> GetLatestAsync(string phoneE164, string purpose, CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM phone_verification_codes
            WHERE phone_e164 = @PhoneE164 AND purpose = @Purpose
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PhoneVerificationCode>(
            new CommandDefinition(sql, new { PhoneE164 = phoneE164, Purpose = purpose }, cancellationToken: ct));
    }

    public async Task IncrementAttemptsAsync(long id, CancellationToken ct = default)
    {
        const string sql = "UPDATE phone_verification_codes SET attempts = attempts + 1 WHERE id = @Id;";
        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task MarkConsumedAsync(long id, DateTime consumedAt, CancellationToken ct = default)
    {
        const string sql = "UPDATE phone_verification_codes SET consumed_at = @ConsumedAt WHERE id = @Id;";
        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, ConsumedAt = consumedAt }, cancellationToken: ct));
    }
}
