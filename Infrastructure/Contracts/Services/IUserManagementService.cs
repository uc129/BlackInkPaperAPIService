using Application.DTOs.Products;
using Application.DTOs.UserAuth;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IUserManagementService
{
    Task<ServiceResponse<PagedResultDto<UserSummaryDto>>> GetAllUsersAsync(AdminUserSearchRequest request, CancellationToken ct = default);
    Task<ServiceResponse<UserSummaryDto>> GetUserByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResponse<UserSummaryDto>> UpdateUserRolesAsync(string id, UpdateUserRolesRequest request, CancellationToken ct = default);
}
