using Dapper;
using Domain.Entities;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class AuditLogRepository(IDapperContext dapperContext) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO AuditLogs (UserId, UserEmail, Method, Path, StatusCode, IpAddress, OccurredAt)
            VALUES (@UserId, @UserEmail, @Method, @Path, @StatusCode, @IpAddress, @OccurredAt);
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, entry);
    }
}
