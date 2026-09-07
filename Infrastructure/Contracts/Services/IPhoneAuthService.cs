using Application.DTOs.UserAuth;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IPhoneAuthService
{
    Task<ServiceResponse<StartPhoneAuthResponse>> StartAsync(
        StartPhoneAuthRequest request, string? requestIp, CancellationToken ct = default);

    /// <summary>
    /// Sends a code for the link flow. Kept distinct from <see cref="StartAsync"/> so a code
    /// issued to add a number to an existing account cannot be redeemed to log in as a new
    /// one, or the reverse.
    /// </summary>
    Task<ServiceResponse<StartPhoneAuthResponse>> StartPhoneLinkAsync(
        StartPhoneAuthRequest request, string? requestIp, CancellationToken ct = default);

    Task<ServiceResponse<AuthResponse>> VerifyAsync(
        VerifyPhoneAuthRequest request, CancellationToken ct = default);

    /// <summary>
    /// Attaches a verified phone number to the signed-in account, so someone who registered
    /// by email can start logging in by phone without creating a second account.
    /// </summary>
    Task<ServiceResponse<string>> LinkPhoneAsync(
        string userId, VerifyPhoneAuthRequest request, CancellationToken ct = default);
}
