using Application.DTOs.Contact;
using Dapper;
using Domain.Aggregates.Contact;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class ContactRepository(IDapperContext dapperContext) : IContactRepository
{
    public async Task<int> AddAsync(ContactSubmissionAggregate submission, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO contact_submissions (name, email, subject, message, submitted_at)
            VALUES (@Name, @Email, @Subject, @Message, @SubmittedAt)
            RETURNING id;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            submission.Name,
            submission.Email,
            submission.Subject,
            submission.Message,
            submission.SubmittedAt
        });
    }

    public async Task<ContactSubmissionAggregate?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM contact_submissions WHERE id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ContactSubmissionAggregate>(sql, new { Id = id });
    }

    public async Task<(IEnumerable<ContactSubmissionAggregate> Items, int TotalCount)> SearchAsync(
        ContactSubmissionSearchRequest request, CancellationToken ct = default)
    {
        var whereClause = request.IsResolved.HasValue
            ? "WHERE is_resolved = @IsResolved"
            : string.Empty;

        var sql = $"""
            SELECT COUNT(1) FROM contact_submissions {whereClause};

            SELECT * FROM contact_submissions
            {whereClause}
            ORDER BY submitted_at DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.IsResolved,
            PageSize = request.PageSize,
            Offset = (request.Page - 1) * request.PageSize
        });

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<ContactSubmissionAggregate>();

        return (items, totalCount);
    }

    public async Task MarkResolvedAsync(int id, string? notes, DateTime resolvedAt, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE contact_submissions
            SET is_resolved = TRUE, resolved_at = @ResolvedAt, resolved_notes = @Notes
            WHERE id = @Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, ResolvedAt = resolvedAt, Notes = notes });
    }
}
