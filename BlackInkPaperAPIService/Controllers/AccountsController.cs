using Application.DTOs.UserAuth;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers
{
    [ApiController]
[ApiVersion("1.0")]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AccountsController(
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService tokenService,
        ITokenBlackListRepo tokenblacklist,
        IEmailService emailService,
        IRefreshTokenRepository refreshTokenRepo,
        IConfiguration config) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string[] allowedRoles = ["Artist", "User"];

            if (!allowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
                return this.ToApiResult(ServiceResponse<string>.Fail($"User cannot be assigned to the role: {request.Role}", statusCode: 400));

            var user = new AppIdentityUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<string>.Fail(errors, statusCode: 400));
            }

            if (await roleManager.RoleExistsAsync(request.Role))
                await userManager.AddToRoleAsync(user, request.Role);

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await emailService.SendAsync(
                request.Email,
                "Confirm your email",
                $"<p>Use this token to confirm your email: <code>{Uri.EscapeDataString(token)}</code></p>" +
                $"<p>Or call: POST /api/accounts/confirm-email with your email and this token.</p>",
                HttpContext.RequestAborted);

            return this.ToApiResult(ServiceResponse<string>.Ok("User registered successfully", "Registration Successful", 201));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return this.ToApiResult(ServiceResponse<AuthResponse>.Fail("Invalid credentials", statusCode: 401));

            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.GenerateToken(user, roles);
            var refreshToken = await refreshTokenRepo.CreateAsync(user.Id, HttpContext.RequestAborted);
            var expiresIn = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m * 60 : 3600;

            return this.ToApiResult(ServiceResponse<AuthResponse>.Ok(
                new AuthResponse(true, accessToken, "Login Successful", refreshToken, expiresIn)));
        }

        [HttpPost("logout")]
        [Authorize] // Only logged-in users can log out
        public async Task<IActionResult> Logout()
        {
             await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // For JWT, we simply return a success message. 
            // The Frontend is responsible for removing the token from its storage.
            return this.ToApiResult(ServiceResponse<string>.Ok("Logged out successfully. Please remove your token."));
        }

        [HttpPost("logout-secure")]
        [Authorize]
        public async Task<IActionResult> SecureLogout()
        {
            var tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expiryClaim = User.FindFirst("exp")?.Value;

            if (tokenId != null && expiryClaim != null)
            {
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryClaim)).UtcDateTime;
                var response = await tokenblacklist.AddTokenToBlackList(tokenId, expiryDate);
                return this.ToApiResult(response);
            }
            return this.ToApiResult(ServiceResponse<string>.Fail("User Claim Not Found!"));
        }

        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<UserProfileDto>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found"));

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            var dto = new UserProfileDto(user.Id, user.Email ?? string.Empty, user.FullName ?? string.Empty, user.ArtistPortfolioUrl, roles, user.EmailConfirmed);
            return this.ToApiResult(ServiceResponse<UserProfileDto>.Ok(dto));
        }

        [HttpPatch("profile")]
        [Authorize]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<UserProfileDto>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found"));

            user.FullName = request.FullName;
            user.ArtistPortfolioUrl = request.ArtistPortfolioUrl;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<UserProfileDto>.Fail(errors, statusCode: 400));
            }

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            var dto = new UserProfileDto(user.Id, user.Email ?? string.Empty, user.FullName ?? string.Empty, user.ArtistPortfolioUrl, roles, user.EmailConfirmed);
            return this.ToApiResult(ServiceResponse<UserProfileDto>.Ok(dto, "Profile updated."));
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<string>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found"));

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<string>.Fail(errors, statusCode: 400, errorCode: "password_change_failed"));
            }

            return this.ToApiResult(ServiceResponse<string>.Ok("Password changed successfully."));
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            // Always return 200 to avoid leaking whether the email exists.
            if (user is not null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var link = $"reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
                await emailService.SendAsync(
                    request.Email,
                    "Reset your password",
                    $"<p>Click the link to reset your password: <a href=\"{link}\">{link}</a></p>",
                    HttpContext.RequestAborted);
            }

            return this.ToApiResult(ServiceResponse<string>.Ok("If that email is registered, a reset link has been sent."));
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return this.ToApiResult(ServiceResponse<string>.Fail("Invalid reset request.", statusCode: 400, errorCode: "invalid_reset_token"));

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<string>.Fail(errors, statusCode: 400, errorCode: "password_reset_failed"));
            }

            return this.ToApiResult(ServiceResponse<string>.Ok("Password reset successfully."));
        }

        [HttpPost("refresh")]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var stored = await refreshTokenRepo.GetByTokenAsync(request.RefreshToken, HttpContext.RequestAborted);
            if (stored is null || !stored.IsActive)
                return this.ToApiResult(ServiceResponse<AuthResponse>.Fail("Invalid or expired refresh token.", statusCode: 401, errorCode: "invalid_refresh_token"));

            var user = await userManager.FindByIdAsync(stored.UserId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<AuthResponse>.Fail("User not found.", statusCode: 401, errorCode: "user_not_found"));

            // Rotate: revoke old token, issue new one
            await refreshTokenRepo.RevokeAsync(request.RefreshToken, HttpContext.RequestAborted);
            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.GenerateToken(user, roles);
            var newRefreshToken = await refreshTokenRepo.CreateAsync(user.Id, HttpContext.RequestAborted);
            var expiresIn = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m * 60 : 3600;

            return this.ToApiResult(ServiceResponse<AuthResponse>.Ok(
                new AuthResponse(true, accessToken, "Token refreshed.", newRefreshToken, expiresIn)));
        }

        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return this.ToApiResult(ServiceResponse<string>.Fail("Invalid confirmation request.", statusCode: 400, errorCode: "invalid_confirmation"));

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<string>.Fail(errors, statusCode: 400, errorCode: "email_confirmation_failed"));
            }

            return this.ToApiResult(ServiceResponse<string>.Ok("Email confirmed successfully."));
        }
    }
}
