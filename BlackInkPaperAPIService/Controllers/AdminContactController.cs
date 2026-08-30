using Application.DTOs.Contact;
using Application.DTOs.Products;
using Asp.Versioning;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/admin/contact")]
public class AdminContactController(
    IContactApplicationService contactApplicationService,
    ILogger<AdminContactController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDto<ContactSubmissionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissions(
        [FromQuery] ContactSubmissionSearchRequest request,
        CancellationToken cancellationToken)
        => this.ToApiResult(await contactApplicationService.GetSubmissionsAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ContactSubmissionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await contactApplicationService.GetByIdAsync(id, cancellationToken);
        if (!response.Success) logger.LogInformation("Contact submission lookup failed for id {Id}.", id);
        return this.ToApiResult(response);
    }

    [HttpPatch("{id:int}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(
        int id,
        [FromBody] ResolveContactSubmissionRequest request,
        CancellationToken cancellationToken)
        => this.ToApiResult(await contactApplicationService.ResolveAsync(id, request, cancellationToken));
}
