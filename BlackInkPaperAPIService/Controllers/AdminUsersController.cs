using Application.DTOs.UserAuth;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public class AdminUsersController(
    IUserManagementService userManagementService,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AdminUserSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await userManagementService.GetAllUsersAsync(request, cancellationToken);
        return this.ToApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var response = await userManagementService.GetUserByIdAsync(id, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin user lookup failed for id {UserId}.", id);
        return this.ToApiResult(response);
    }

    [HttpPatch("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(
        string id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userManagementService.UpdateUserRolesAsync(id, request, cancellationToken);
        return this.ToApiResult(response);
    }
}
