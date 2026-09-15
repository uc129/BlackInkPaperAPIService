using Domain.Entities;

namespace Infrastructure.Contracts.Repositories;

public interface IPhoneVerificationRepository
{
    /// <summary>
    /// How many codes have been sent to this number since <paramref name="since"/>. Backs the
    /// per-number send throttle, which is the limit that actually caps the messaging bill —
    /// the middleware rate limiter can only see the IP.
    /// </summary>
    Task<int> CountSendsSinceAsync(string phoneE164, DateTime since, CancellationToken ct = default);

    /// <summary>
    /// Invalidates any live challenge for the number, so that issuing a new code retires the
    /// old one instead of leaving several valid at once.
    /// </summary>
    Task ExpireLiveChallengesAsync(string phoneE164, DateTime now, CancellationToken ct = default);

    Task<long> AddAsync(PhoneVerificationCode code, CancellationToken ct = default);

    /// <summary>The newest challenge for a number, redeemable or not — the caller decides.</summary>
    Task<PhoneVerificationCode?> GetLatestAsync(string phoneE164, string purpose, CancellationToken ct = default);

    Task IncrementAttemptsAsync(long id, CancellationToken ct = default);

    Task MarkConsumedAsync(long id, DateTime consumedAt, CancellationToken ct = default);
}
