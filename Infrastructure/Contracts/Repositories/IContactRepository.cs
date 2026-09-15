using Application.DTOs.Contact;
using Domain.Aggregates.Contact;

namespace Infrastructure.Contracts.Repositories;

public interface IContactRepository
{
    Task<int> AddAsync(ContactSubmissionAggregate submission, CancellationToken ct = default);
    Task<ContactSubmissionAggregate?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<ContactSubmissionAggregate> Items, int TotalCount)> SearchAsync(
        ContactSubmissionSearchRequest request, CancellationToken ct = default);
    Task MarkResolvedAsync(int id, string? notes, DateTime resolvedAt, CancellationToken ct = default);
}
