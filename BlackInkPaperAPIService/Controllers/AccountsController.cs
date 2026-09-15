using Application.DTOs.UserAuth;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
using Infrastructure.Services.Email;
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
        AccountLinkBuilder linkBuilder,
        IConfiguration config) : ControllerBase
    {
        [HttpPost("register")]
        [ProducesResponseType<string>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string[] allowedRoles = ["Artist", "User"];

            if (!allowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
                return this.ToApiResult(ServiceResponse<string>.Fail($"User cannot be assigned to the role: {request.Role}", statusCode: 400));

            // Identity's built-in duplicate-email check no longer runs: User.RequireUniqueEmail
            // has to stay false so phone-only accounts (which have no email) can be created at
            // all. Catching the collision here keeps the 409 — without it the unique partial
            // index on NormalizedEmail surfaces the same condition as a 500.
            if (await userManager.FindByEmailAsync(request.Email) is not null)
                return this.ToApiResult(ServiceResponse<string>.Fail(
                    "An account with that email already exists.", statusCode: 409, errorCode: "email_already_registered"));

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
            var confirmLink = linkBuilder.ConfirmEmail(request.Email, token);
            var confirmEmail = AccountEmailTemplates.ConfirmEmail(confirmLink, request.FullName);
            await emailService.SendAsync(
                request.Email, confirmEmail.Subject, confirmEmail.HtmlBody, HttpContext.RequestAborted);

            return this.ToApiResult(ServiceResponse<string>.Ok("User registered successfully", "Registration Successful", 201));
        }

        [HttpPost("login")]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
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
        [Authorize]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            return this.ToApiResult(ServiceResponse<string>.Ok("Logged out successfully. Please remove your token."));
        }

        [HttpPost("logout-secure")]
        [Authorize]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SecureLogout()
        {
            // Both halves of the credential have to go. Blacklisting the access token alone
            // leaves the refresh token live for its full 30 days, so anyone holding it could
            // mint a new access token immediately after the "secure" logout.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
                await refreshTokenRepo.RevokeAllForUserAsync(userId, HttpContext.RequestAborted);

            var tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expiryClaim = User.FindFirst("exp")?.Value;

            if (tokenId != null && expiryClaim != null)
            {
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryClaim)).UtcDateTime;
                var response = await tokenblacklist.AddTokenToBlackList(tokenId, expiryDate);
                return this.ToApiResult(response);
            }

            // Refresh tokens are already revoked above, so the session is ended either way —
            // only the access token survives, and just until it expires.
            return this.ToApiResult(ServiceResponse<string>.Fail(
                "User Claim Not Found!", statusCode: 400, errorCode: "token_claims_missing"));
        }

        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<UserProfileDto>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found"));

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            var dto = new UserProfileDto(user.Id, user.Email, user.PhoneNumber, user.FullName ?? string.Empty, user.ArtistPortfolioUrl, roles, user.EmailConfirmed, user.PhoneNumberConfirmed);
            return this.ToApiResult(ServiceResponse<UserProfileDto>.Ok(dto));
        }

        [HttpPatch("profile")]
        [Authorize]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
            var dto = new UserProfileDto(user.Id, user.Email, user.PhoneNumber, user.FullName ?? string.Empty, user.ArtistPortfolioUrl, roles, user.EmailConfirmed, user.PhoneNumberConfirmed);
            return this.ToApiResult(ServiceResponse<UserProfileDto>.Ok(dto, "Profile updated."));
        }

        /// <summary>
        /// Adds an email to an account that was created from a phone number, giving it a
        /// second way to sign in and a route for receipts. The address is unconfirmed until
        /// the emailed token is redeemed, exactly as at registration.
        /// </summary>
        [HttpPost("email/link")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> LinkEmail([FromBody] LinkEmailRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null)
                return this.ToApiResult(ServiceResponse<string>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found"));

            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null && existing.Id != user.Id)
                return this.ToApiResult(ServiceResponse<string>.Fail(
                    "That email is already linked to another account.", statusCode: 409, errorCode: "email_already_registered"));

            var setResult = await userManager.SetEmailAsync(user, request.Email);
            if (!setResult.Succeeded)
                return this.ToApiResult(ServiceResponse<string>.Fail(
                    string.Join(", ", setResult.Errors.Select(e => e.Description)),
                    statusCode: 400, errorCode: "email_link_failed"));

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = linkBuilder.ConfirmEmail(request.Email, token);
            var linkedEmail = AccountEmailTemplates.ConfirmLinkedEmail(confirmLink);
            await emailService.SendAsync(
                request.Email, linkedEmail.Subject, linkedEmail.HtmlBody, HttpContext.RequestAborted);

            return this.ToApiResult(ServiceResponse<string>.Ok("Confirmation email sent."));
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
                var link = linkBuilder.ResetPassword(request.Email, token);
                var resetEmail = AccountEmailTemplates.PasswordReset(link);
                await emailService.SendAsync(
                    request.Email, resetEmail.Subject, resetEmail.HtmlBody, HttpContext.RequestAborted);
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
