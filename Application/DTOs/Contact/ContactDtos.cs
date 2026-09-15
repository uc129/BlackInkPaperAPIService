using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Contact;

public record SubmitContactRequest(
    [Required][StringLength(200, MinimumLength = 1)] string Name,
    [Required][EmailAddress][StringLength(320)] string Email,
    [Required][StringLength(500, MinimumLength = 1)] string Subject,
    [Required][StringLength(5000, MinimumLength = 1)] string Message);

public record ContactSubmissionDto(
    int Id,
    string Name,
    string Email,
    string Subject,
    string Message,
    DateTime SubmittedAt,
    bool IsResolved,
    DateTime? ResolvedAt,
    string? ResolvedNotes);

public record ContactSubmissionSearchRequest(
    int Page = 1,
    int PageSize = 20,
    bool? IsResolved = null);

public record ResolveContactSubmissionRequest(string? Notes);
