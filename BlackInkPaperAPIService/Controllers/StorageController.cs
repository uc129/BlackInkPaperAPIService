using Application.DTOs.Storage;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/storage")]
[EnableRateLimiting("storage")]
public class StorageController(IStorageService storageService) : ControllerBase
{
    private static readonly HashSet<string> AllowedParams =
        new(StringComparer.OrdinalIgnoreCase) { "folder", "tags", "context", "resource_type" };

    [HttpPost("sign")]
    [ProducesResponseType<StorageSignatureResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GenerateSignature([FromBody] StorageSignatureRequest request)
    {
        var invalid = request.Parameters.Keys
            .Where(k => !AllowedParams.Contains(k))
            .ToList();

        if (invalid.Count > 0)
            return this.ToApiResult(ServiceResponse<string>.Fail(
                $"Disallowed parameter(s): {string.Join(", ", invalid)}. " +
                $"Allowed: {string.Join(", ", AllowedParams)}.",
                statusCode: 400, errorCode: "INVALID_UPLOAD_PARAMS"));

        var result = storageService.GenerateUploadSignature(request.Parameters);
        return Ok(ServiceResponse<StorageSignatureResponse>.Ok(result));
    }

    [HttpDelete("{publicId}")]
    [Authorize(Roles = "Admin,Artist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string publicId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return this.ToApiResult(ServiceResponse<string>.Fail("publicId is required.", statusCode: 400, errorCode: "MISSING_PUBLIC_ID"));

        var deleted = await storageService.DeleteAsync(publicId, ct);
        if (!deleted)
            return this.ToApiResult(ServiceResponse<string>.Fail("Asset not found or could not be deleted.", statusCode: 404, errorCode: "ASSET_NOT_FOUND"));

        return NoContent();
    }
}
