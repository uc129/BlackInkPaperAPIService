using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserAuth;

public record AdminUserSearchRequest(
    int Page = 1,
    [Range(1, 100)] int PageSize = 50,
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
