using Domain.Entities;

namespace Infrastructure.Contracts.Repositories;

public interface IRefreshTokenRepository
{
    Task<string> CreateAsync(string userId, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken ct = default);
    Task RevokeAsync(string plainToken, CancellationToken ct = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken ct = default);
}
