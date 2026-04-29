namespace Application.DTOs.UserAuth;

public record AdminUserSearchRequest(
    int Page = 1,
    int PageSize = 50,
    string? Role = null,
    string? Query = null);

public record UserSummaryDto(
    string Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    bool IsActive);

public record UpdateUserRolesRequest(IReadOnlyList<string> Roles);
