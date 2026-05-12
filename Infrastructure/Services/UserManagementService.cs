using Application.DTOs.Products;
using Application.DTOs.UserAuth;
using Common.YourProject.Models;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserManagementService(
    UserManager<AppIdentityUser> userManager) : IUserManagementService
{
    public async Task<ServiceResponse<PagedResultDto<UserSummaryDto>>> GetAllUsersAsync(
        AdminUserSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var query = userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var q = request.Query.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(q)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(q)));
            }

            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var totalCount = await query.CountAsync(ct);
            var offset = (page - 1) * pageSize;
            var users = await query
                .OrderBy(u => u.Email)
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(ct);

            var dtos = new List<UserSummaryDto>(users.Count);
            foreach (var user in users)
            {
                var roles = (await userManager.GetRolesAsync(user)).ToList();
                if (!string.IsNullOrWhiteSpace(request.Role) &&
                    !roles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
                    continue;

                dtos.Add(ToDto(user, roles));
            }

            return ServiceResponse<PagedResultDto<UserSummaryDto>>.Ok(
                new PagedResultDto<UserSummaryDto>(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResultDto<UserSummaryDto>>.Fail(
                "Unable to fetch users.", ex.ToString(), 500, "user_list_failed");
        }
    }

    public async Task<ServiceResponse<UserSummaryDto>> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return ServiceResponse<UserSummaryDto>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found");

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            return ServiceResponse<UserSummaryDto>.Ok(ToDto(user, roles));
        }
        catch (Exception ex)
        {
            return ServiceResponse<UserSummaryDto>.Fail("Unable to fetch user.", ex.ToString(), 500, "user_read_failed");
        }
    }

    public async Task<ServiceResponse<UserSummaryDto>> UpdateUserRolesAsync(
        string id, UpdateUserRolesRequest request, CancellationToken ct = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return ServiceResponse<UserSummaryDto>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found");

            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRolesAsync(user, request.Roles);

            var updatedRoles = (await userManager.GetRolesAsync(user)).ToList();
            return ServiceResponse<UserSummaryDto>.Ok(ToDto(user, updatedRoles), "Roles updated.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<UserSummaryDto>.Fail("Unable to update roles.", ex.ToString(), 500, "role_update_failed");
        }
    }

    private static UserSummaryDto ToDto(AppIdentityUser user, IReadOnlyList<string> roles)
        => new(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName ?? string.Empty,
            roles,
            DateTime.MinValue, // Identity doesn't store CreatedAt; placeholder
            !user.LockoutEnabled || user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow);
}
