using Application.DTOs.UserAuth;
using Common.YourProject.Models;
using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Passwordless phone login: a code is sent to a number, and proving possession of that
/// number is the whole credential. There is no password on a phone-first account, so there is
/// none to reset, forget, or leak.
/// </summary>
public sealed class PhoneAuthService(
    UserManager<AppIdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IPhoneVerificationRepository verificationRepository,
    IOtpCodeHasher codeHasher,
    IOtpDeliveryService otpDelivery,
    IJwtTokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo,
    IOptions<OtpOptions> otpOptions,
    IConfiguration config,
    ILogger<PhoneAuthService> logger) : IPhoneAuthService
{
    private const string LoginPurpose = "Login";
    private const string LinkPurpose = "LinkPhone";
    private const string CustomerRole = "User";

    /// <summary>
    /// One message for every way a code can be unusable — wrong, expired, already used, or
    /// out of attempts. Distinguishing them would tell an attacker which numbers have a
    /// challenge in flight and how many guesses remain.
    /// </summary>
    private const string InvalidCodeMessage = "That code is not valid or has expired. Request a new one.";

    private readonly OtpOptions options = otpOptions.Value;

    public Task<ServiceResponse<StartPhoneAuthResponse>> StartAsync(
        StartPhoneAuthRequest request, string? requestIp, CancellationToken ct = default)
        => SendChallengeAsync(request, LoginPurpose, requestIp, ct);

    public Task<ServiceResponse<StartPhoneAuthResponse>> StartPhoneLinkAsync(
        StartPhoneAuthRequest request, string? requestIp, CancellationToken ct = default)
        => SendChallengeAsync(request, LinkPurpose, requestIp, ct);

    private async Task<ServiceResponse<StartPhoneAuthResponse>> SendChallengeAsync(
        StartPhoneAuthRequest request, string purpose, string? requestIp, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (!normalized.Success)
            return ServiceResponse<StartPhoneAuthResponse>.Fail(
                normalized.Error!, statusCode: 400, errorCode: "invalid_phone_number");

        var phone = normalized.Value;
        var now = DateTime.UtcNow;

        // The per-number throttle, not the per-IP rate limiter, is what caps the messaging
        // bill: an attacker rotating IPs against one number would sail past an IP limit.
        var windowStart = now.AddMinutes(-options.SendWindowMinutes);
        var recentSends = await verificationRepository.CountSendsSinceAsync(phone, windowStart, ct);
        if (recentSends >= options.MaxSendsPerWindow)
        {
            return ServiceResponse<StartPhoneAuthResponse>.Fail(
                "Too many codes requested. Please wait a few minutes before trying again.",
                statusCode: 429, errorCode: "otp_send_throttled");
        }

        await verificationRepository.ExpireLiveChallengesAsync(phone, now, ct);

        var code = codeHasher.GenerateCode();
        var delivery = await otpDelivery.SendAsync(phone, code, ct);

        // Recorded whether or not delivery succeeded, so repeated failures still count against
        // the throttle rather than offering a free retry loop.
        await verificationRepository.AddAsync(new PhoneVerificationCode
        {
            PhoneE164 = phone,
            CodeHash = codeHasher.Hash(phone, code),
            Purpose = purpose,
            Attempts = 0,
            MaxAttempts = options.MaxVerifyAttempts,
            ExpiresAt = now.AddMinutes(options.ExpiryMinutes),
            CreatedAt = now,
            RequestIp = requestIp,
            Channel = delivery.Success ? delivery.Channel.ToString() : null
        }, ct);

        if (!delivery.Success)
        {
            return ServiceResponse<StartPhoneAuthResponse>.Fail(
                delivery.Error ?? "We could not send your code.",
                statusCode: 502, errorCode: "otp_delivery_failed");
        }

        return ServiceResponse<StartPhoneAuthResponse>.Ok(
            new StartPhoneAuthResponse(delivery.Channel.ToString(), options.ExpiryMinutes * 60),
            "Verification code sent.");
    }

    public async Task<ServiceResponse<AuthResponse>> VerifyAsync(
        VerifyPhoneAuthRequest request, CancellationToken ct = default)
    {
        var challenge = await ResolveChallengeAsync(request, LoginPurpose, ct);
        if (!challenge.Success)
            return ServiceResponse<AuthResponse>.Fail(
                challenge.Message, statusCode: challenge.StatusCode, errorCode: challenge.ErrorCode);

        var phone = challenge.Data!.PhoneE164;
        var user = await FindByPhoneAsync(phone, ct);

        if (user is null)
        {
            var created = await CreateCustomerAsync(phone, request.FullName, challenge.Data.Channel, ct);
            if (!created.Success)
                return ServiceResponse<AuthResponse>.Fail(
                    created.Message, statusCode: created.StatusCode, errorCode: created.ErrorCode);

            user = created.Data!;
        }
        else if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<ServiceResponse<string>> LinkPhoneAsync(
        string userId, VerifyPhoneAuthRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResponse<string>.Fail("User not found.", statusCode: 404, errorCode: "user_not_found");

        var challenge = await ResolveChallengeAsync(request, LinkPurpose, ct);
        if (!challenge.Success)
            return ServiceResponse<string>.Fail(
                challenge.Message, statusCode: challenge.StatusCode, errorCode: challenge.ErrorCode);

        var phone = challenge.Data!.PhoneE164;

        // The number may already belong to another account — either one created from it, or
        // another email account that linked it first. Both are found by phone, not by login
        // handle, which is why this cannot go through FindByNameAsync.
        var existing = await FindByPhoneAsync(phone, ct);
        if (existing is not null && existing.Id != user.Id)
            return ServiceResponse<string>.Fail(
                "That phone number is already linked to another account.",
                statusCode: 409, errorCode: "phone_already_linked");

        user.PhoneNumber = phone;
        user.PhoneNumberConfirmed = true;
        user.WhatsAppOptInAt ??= DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResponse<string>.Fail(
                string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: 400, errorCode: "phone_link_failed");

        return ServiceResponse<string>.Ok("Phone number linked.");
    }

    /// <summary>
    /// Resolves an account by phone number.
    ///
    /// This must query PhoneNumber rather than the login handle. An account created from a
    /// number has UserName set to it, but an email account that later linked the same number
    /// keeps its email as UserName — looking up by handle would miss that user and silently
    /// create a second, empty account for someone who already has one.
    /// </summary>
    private Task<AppIdentityUser?> FindByPhoneAsync(string phoneE164, CancellationToken ct)
        => userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneE164, ct);

    /// <summary>
    /// Normalizes the number, finds its newest challenge, and checks the code — incrementing
    /// the attempt count on a miss so a wrong guess costs something.
    /// </summary>
    private async Task<ServiceResponse<PhoneVerificationCode>> ResolveChallengeAsync(
        VerifyPhoneAuthRequest request, string purpose, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (!normalized.Success)
            return ServiceResponse<PhoneVerificationCode>.Fail(
                normalized.Error!, statusCode: 400, errorCode: "invalid_phone_number");

        var phone = normalized.Value;
        var challenge = await verificationRepository.GetLatestAsync(phone, purpose, ct);

        if (challenge is null || !challenge.IsRedeemable)
            return ServiceResponse<PhoneVerificationCode>.Fail(
                InvalidCodeMessage, statusCode: 400, errorCode: "invalid_or_expired_code");

        if (!codeHasher.Verify(phone, request.Code, challenge.CodeHash))
        {
            await verificationRepository.IncrementAttemptsAsync(challenge.Id, ct);
            return ServiceResponse<PhoneVerificationCode>.Fail(
                InvalidCodeMessage, statusCode: 400, errorCode: "invalid_or_expired_code");
        }

        // Consumed before the caller acts on it, so the same code cannot be replayed by a
        // second request that arrives while the first is still working.
        await verificationRepository.MarkConsumedAsync(challenge.Id, DateTime.UtcNow, ct);
        return ServiceResponse<PhoneVerificationCode>.Ok(challenge);
    }

    private async Task<ServiceResponse<AppIdentityUser>> CreateCustomerAsync(
        string phone, string? fullName, string? channel, CancellationToken ct)
    {
        var user = new AppIdentityUser
        {
            // UserName carries the login handle, which is now either an email or a phone.
            // Using it for the phone inherits Identity's existing unique index on
            // NormalizedUserName, so two accounts cannot claim the same number.
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            FullName = fullName,

            // Asking for a code over WhatsApp and using it is consent to transactional
            // WhatsApp, and only that. Marketing consent is a separate, explicit act and is
            // deliberately left null here.
            WhatsAppOptInAt = string.Equals(channel, nameof(OtpChannel.WhatsApp), StringComparison.Ordinal)
                ? DateTime.UtcNow
                : null
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Phone signup failed to create a user. Errors={Errors}", errors);
            return ServiceResponse<AppIdentityUser>.Fail(
                errors, statusCode: 400, errorCode: "phone_signup_failed");
        }

        if (await roleManager.RoleExistsAsync(CustomerRole))
            await userManager.AddToRoleAsync(user, CustomerRole);

        return ServiceResponse<AppIdentityUser>.Ok(user);
    }

    private async Task<ServiceResponse<AuthResponse>> IssueTokensAsync(AppIdentityUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateToken(user, roles);
        var refreshToken = await refreshTokenRepo.CreateAsync(user.Id, ct);
        var expiresIn = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m * 60 : 3600;

        return ServiceResponse<AuthResponse>.Ok(
            new AuthResponse(true, accessToken, "Login Successful", refreshToken, expiresIn));
    }
}
