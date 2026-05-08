using Domain.Entities;

namespace Infrastructure.Contracts.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);
}
