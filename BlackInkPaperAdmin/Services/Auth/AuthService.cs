using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.UserAuth;
using Microsoft.JSInterop;

namespace BlackInkPaperAdmin.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly TokenStore _store;
    private readonly IJSRuntime _js;

    public AuthService(HttpClient http, TokenStore store, IJSRuntime js)
    {
        _http = http;
        _store = store;
        _js = js;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/accounts/login",
                new LoginRequest(email, password));
            var result = await resp.Content.ReadFromJsonAsync<ServiceResponse<AuthResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Success == true && result.Data?.Token is { } token)
            {
                Apply(token);
                await _js.InvokeVoidAsync("localStorage.setItem", "bip_admin_token", token);
                return (true, null);
            }

            return (false, result?.Message ?? "Login failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        try { await _http.PostAsync("api/accounts/logout-secure", null); }
        catch { }
        _store.Clear();
        _http.DefaultRequestHeaders.Authorization = null;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", "bip_admin_token"); } catch { }
    }

    public Task<bool> InitializeAsync(string token)
    {
        if (IsExpired(token)) return Task.FromResult(false);
        Apply(token);
        return Task.FromResult(true);
    }

    public UserInfo? GetCurrentUser()
    {
        if (_store.Token is not { } token) return null;
        return ParseUserInfo(token);
    }

    private void Apply(string token)
    {
        _store.Set(token);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static bool IsExpired(string token)
    {
        var payload = DecodePayload(token);
        if (payload is null) return true;
        if (!payload.Value.TryGetProperty("exp", out var exp)) return false;
        return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) < DateTimeOffset.UtcNow;
    }

    private static UserInfo? ParseUserInfo(string token)
    {
        var payload = DecodePayload(token);
        if (payload is null) return null;
        var j = payload.Value;

        var name = Claim(j, "FullName") ?? Claim(j, "unique_name") ?? Claim(j, "name");
        var email = Claim(j, "email") ?? "";
        var role = Claim(j, "role") ?? "";

        if (string.IsNullOrEmpty(name))
            name = email.Contains('@') ? email[..email.IndexOf('@')] : "Admin";

        return new UserInfo(name, email, role);
    }

    private static JsonElement? DecodePayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var pad = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(pad.Replace('-', '+').Replace('_', '/'));
            return JsonSerializer.Deserialize<JsonElement>(bytes);
        }
        catch { return null; }
    }

    private static string? Claim(JsonElement json, string key)
    {
        if (!json.TryGetProperty(key, out var val)) return null;
        return val.ValueKind == JsonValueKind.String ? val.GetString() : null;
    }

}
