namespace BlackInkPaperAdmin.Services.Auth;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<bool> InitializeAsync(string token);
    UserInfo? GetCurrentUser();
}
