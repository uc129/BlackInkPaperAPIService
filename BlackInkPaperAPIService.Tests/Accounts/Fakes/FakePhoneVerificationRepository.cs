using Domain.Entities;
using Infrastructure.Contracts.Repositories;

namespace BlackInkPaperAPIService.Tests.Accounts.Fakes;

internal sealed class FakePhoneVerificationRepository : IPhoneVerificationRepository
{
    // ── Configurable handlers ────────────────────────────────────────────────
    public Func<string, DateTime, Task<int>> CountSendsSinceHandler { get; set; }
        = (_, _) => Task.FromResult(0);
    public Func<string, string, Task<PhoneVerificationCode?>> GetLatestHandler { get; set; }
        = (_, _) => Task.FromResult<PhoneVerificationCode?>(null);

    // ── Captured values ──────────────────────────────────────────────────────
    public PhoneVerificationCode? AddedCode { get; private set; }
    public string? ExpiredForPhone { get; private set; }
    public long? IncrementedAttemptsForId { get; private set; }
    public long? ConsumedId { get; private set; }

    public Task<int> CountSendsSinceAsync(string phoneE164, DateTime since, CancellationToken ct = default)
        => CountSendsSinceHandler(phoneE164, since);

    public Task ExpireLiveChallengesAsync(string phoneE164, DateTime now, CancellationToken ct = default)
    {
        ExpiredForPhone = phoneE164;
        return Task.CompletedTask;
    }

    public Task<long> AddAsync(PhoneVerificationCode code, CancellationToken ct = default)
    {
        AddedCode = code;
        return Task.FromResult(1L);
    }

    public Task<PhoneVerificationCode?> GetLatestAsync(string phoneE164, string purpose, CancellationToken ct = default)
        => GetLatestHandler(phoneE164, purpose);

    public Task IncrementAttemptsAsync(long id, CancellationToken ct = default)
    {
        IncrementedAttemptsForId = id;
        return Task.CompletedTask;
    }

    public Task MarkConsumedAsync(long id, DateTime consumedAt, CancellationToken ct = default)
    {
        ConsumedId = id;
        return Task.CompletedTask;
    }
}
