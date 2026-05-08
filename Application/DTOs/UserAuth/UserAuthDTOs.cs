using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserAuth
{
    public record RegisterRequest(
        [Required][EmailAddress][MaxLength(256)] string Email,
        [Required][MinLength(8)][MaxLength(128)] string Password,
        [Required][MaxLength(100)] string FullName,
        [Required] string Role);

    public record LoginRequest(
        [Required][EmailAddress] string Email,
        [Required] string Password);

    public record AuthResponse(
        bool Success,
        string Token,
        string Message,
        string? RefreshToken = null,
        int ExpiresIn = 0);

    public record UserProfileDto(
        string Id,
        string Email,
        string FullName,
        string? ArtistPortfolioUrl,
        IReadOnlyList<string> Roles,
        bool EmailConfirmed);

    public record UpdateProfileRequest(
        [Required][MaxLength(100)] string FullName,
        [MaxLength(512)] string? ArtistPortfolioUrl);

    public record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required][MinLength(8)][MaxLength(128)] string NewPassword);

    public record ForgotPasswordRequest(
        [Required][EmailAddress] string Email);

    public record ResetPasswordRequest(
        [Required][EmailAddress] string Email,
        [Required] string Token,
        [Required][MinLength(8)][MaxLength(128)] string NewPassword);

    public record RefreshTokenRequest(
        [Required] string RefreshToken);

    public record TokenResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);

    public record ConfirmEmailRequest(
        [Required][EmailAddress] string Email,
        [Required] string Token);
}
