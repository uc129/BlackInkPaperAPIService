using Infrastructure.Contracts.Repositories;
using System.Security.Claims;

namespace BlackInkPaperAPIService.Middleware;

public class AdminAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (ShouldAudit(context))
        {
            // Fire-and-forget to avoid blocking the response
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = context.RequestServices.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                    await repo.AddAsync(new Domain.Entities.AuditLog
                    {
                        UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                        UserEmail = context.User.FindFirstValue(ClaimTypes.Email),
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value ?? string.Empty,
                        StatusCode = context.Response.StatusCode,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        OccurredAt = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Audit failures must never surface as user-visible errors
                }
            });
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase)
            && (HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
                || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method));
    }
}
