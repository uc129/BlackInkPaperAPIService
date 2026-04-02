using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.UserAuth
{
    public record RegisterRequest(string Email, string Password, string FullName, string Role);

    public record LoginRequest(string Email, string Password);

    public record AuthResponse(bool Success, string Token, string Message);
}
