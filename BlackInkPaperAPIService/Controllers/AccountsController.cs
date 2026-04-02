using Application.DTOs.UserAuth;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
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
                return BadRequest($"User cannot be assigned to the role: {request.Role}");
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
                return BadRequest(result.Errors);

            // 3. Assign Role (ensure role exists first)
            if (await roleManager.RoleExistsAsync(request.Role))
            {
                await userManager.AddToRoleAsync(user, request.Role);
            }

            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized("Invalid credentials");

            var roles = await userManager.GetRolesAsync(user);
            var token = tokenService.GenerateToken(user, roles);

            return Ok(new AuthResponse(true, token, "Login Successful"));
        }

        [HttpPost("logout")]
        [Authorize] // Only logged-in users can log out
        public async Task<IActionResult> Logout()
        {
            // If you were using Cookies, you would clear them here:
            // await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // For JWT, we simply return a success message. 
            // The Frontend is responsible for removing the token from its storage.
            return Ok(new { message = "Logged out successfully. Please remove your token." });
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
                    if(response.Success) return Ok(new { message = "Token invalidated successfully." });
                    else return BadRequest();
            }
            return BadRequest("User Claim Not Found!");
        }
    }
}
