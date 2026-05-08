using System.Security.Cryptography;
using System.Text;
using Dapper;
using Domain.Entities;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class RefreshTokenRepository(IDapperContext dapperContext) : IRefreshTokenRepository
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

    public async Task<string> CreateAsync(string userId, CancellationToken ct = default)
    {
        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Hash(plainToken);
        var now = DateTime.UtcNow;

        const string sql = """
            INSERT INTO RefreshTokens (UserId, TokenHash, ExpiresAt, CreatedAt)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @CreatedAt);
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now.Add(TokenLifetime),
            CreatedAt = now
        });

        return plainToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM RefreshTokens WHERE TokenHash = @TokenHash LIMIT 1;";
        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { TokenHash = Hash(plainToken) });
    }

    public async Task RevokeAsync(string plainToken, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE RefreshTokens SET RevokedAt = @Now WHERE TokenHash = @TokenHash AND RevokedAt IS NULL;
            """;
        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Now = DateTime.UtcNow, TokenHash = Hash(plainToken) });
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE RefreshTokens SET RevokedAt = @Now WHERE UserId = @UserId AND RevokedAt IS NULL;
            """;
        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Now = DateTime.UtcNow, UserId = userId });
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
