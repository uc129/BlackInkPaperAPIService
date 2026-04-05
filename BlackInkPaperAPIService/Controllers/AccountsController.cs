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
using System.IdentityModel.Tokens.Jwt;

namespace BlackInkPaperAPIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController( 
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService tokenService,
        ITokenBlackListRepo tokenblacklist
        ) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string[] allowedRoles = { "Artist", "User" };

            if (!allowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            {
                return this.ToApiResult(ServiceResponse<string>.Fail($"User cannot be assigned to the role: {request.Role}", statusCode: 400));
            }

            // 1. Create User Object
            var user = new AppIdentityUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            // 2. Save to DB via Identity
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.ToApiResult(ServiceResponse<string>.Fail(errors, statusCode: 400));
            }

            // 3. Assign Role (ensure role exists first)
            if (await roleManager.RoleExistsAsync(request.Role))
            {
                await userManager.AddToRoleAsync(user, request.Role);
            }

            return this.ToApiResult(ServiceResponse<string>.Ok("User registered successfully", "Registration Successful", 201));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return this.ToApiResult(ServiceResponse<AuthResponse>.Fail("Invalid credentials", statusCode: 401));

            var roles = await userManager.GetRolesAsync(user);
            var token = tokenService.GenerateToken(user, roles);

            return this.ToApiResult(ServiceResponse<AuthResponse>.Ok(new AuthResponse(true, token, "Login Successful")));
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
            // Extract the Token ID (JTI) from the claims
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
    }
}
