using Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Dapper;

namespace BlackInkPaperAPIService.Middleware
{
    public class TokenBlacklistMiddleware(RequestDelegate next, IDapperContext dbcontext)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Check if the user is even authenticated
            if (context.User.Identity is { IsAuthenticated: true })
            {
                // 2. Extract the JTI (Unique Token ID) from the claims
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    using var connection = dbcontext.CreateConnection();

                    // 3. Fast Dapper check
                    const string sql = "SELECT COUNT(1) FROM TokenBlacklist WHERE TokenId = @Jti";
                    var isBlacklisted = await connection.ExecuteScalarAsync<int>(sql, new { Jti = jti });

                    if (isBlacklisted > 0)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Token has been invalidated. Please log in again." });
                        return;
                    }
                }
            }

            await next(context);
        }
    }
}
