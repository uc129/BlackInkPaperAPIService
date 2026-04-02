using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlackInkPaperAdmin.Services;

public class AdminSession
{
    public string? Token { get; private set; }
    public string DisplayName { get; private set; } = "User";
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public Task EnsureLoadedAsync() => Task.CompletedTask;

    public Task SetTokenAsync(string token)
    {
        Token = token;
        var claims = ParseClaims(token);
        DisplayName = claims.FirstOrDefault(c => c.Type is "unique_name" or ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email)?.Value
            ?? "User";
        Roles = claims.Where(c => c.Type is ClaimTypes.Role or "role").Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Token = null;
        DisplayName = "User";
        Roles = [];
        return Task.CompletedTask;
    }

    private static List<Claim> ParseClaims(string jwt)
    {
        var segments = jwt.Split('.');
        if (segments.Length < 2)
        {
            return [];
        }

        var payload = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var document = JsonDocument.Parse(json);

        var claims = new List<Claim>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var value in property.Value.EnumerateArray())
                {
                    claims.Add(new Claim(property.Name, value.GetString() ?? string.Empty));
                }
            }
            else
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }
        }

        return claims;
    }
}
