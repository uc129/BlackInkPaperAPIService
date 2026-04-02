using Common.YourProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlackInkPaperAPIService.Controllers.Extensions;

public static class ControllerResponseExtensions
{
    public static IActionResult ToApiResult<T>(this ControllerBase controller, ServiceResponse<T> response)
    {
        if (response.Success)
        {
            return controller.StatusCode(response.StatusCode, response);
        }

        var problem = new ProblemDetails
        {
            Title = response.Message,
            Detail = response.TechnicalDetails,
            Status = response.StatusCode
        };

        if (!string.IsNullOrWhiteSpace(response.ErrorCode))
        {
            problem.Extensions["errorCode"] = response.ErrorCode;
        }

        if (response.Metadata.Count > 0)
        {
            problem.Extensions["metadata"] = response.Metadata;
        }

        return controller.StatusCode(response.StatusCode, problem);
    }
}
