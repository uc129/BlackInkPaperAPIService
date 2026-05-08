namespace Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
}
