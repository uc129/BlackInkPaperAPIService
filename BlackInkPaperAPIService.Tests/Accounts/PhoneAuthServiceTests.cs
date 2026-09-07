using BlackInkPaperAPIService.Tests.Accounts.Fakes;
using Application.DTOs.UserAuth;
using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Accounts;

/// <summary>
/// Covers the OTP challenge rules — the security-critical half of phone login.
///
/// UserManager and RoleManager are passed as null: none of the paths tested here reach them,
/// because a challenge that fails to resolve never gets as far as finding or creating a user.
/// Account creation itself is covered by the manual end-to-end run, which needs a real
/// Identity store to mean anything.
/// </summary>
public class PhoneAuthServiceTests
{
    private const string RawPhone = "9876543210";
    private const string CanonicalPhone = "+919876543210";
    private const string Code = "123456";

    private static readonly OtpOptions Options = new()
    {
        HashKey = "test-hash-key",
        CodeLength = 6,
        ExpiryMinutes = 10,
        MaxVerifyAttempts = 5,
        MaxSendsPerWindow = 3,
        SendWindowMinutes = 10
    };

    private static OtpCodeHasher Hasher => new(Microsoft.Extensions.Options.Options.Create(Options));

    private static PhoneAuthService CreateService(
        FakePhoneVerificationRepository repository,
        FakeOtpDeliveryService delivery)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:ExpiryMinutes"] = "60" })
            .Build();

        return new PhoneAuthService(
            userManager: null!,
            roleManager: null!,
            verificationRepository: repository,
            codeHasher: Hasher,
            otpDelivery: delivery,
            tokenService: null!,
            refreshTokenRepo: null!,
            otpOptions: Microsoft.Extensions.Options.Options.Create(Options),
            config: config,
            logger: NullLogger<PhoneAuthService>.Instance);
    }

    private static PhoneVerificationCode Challenge(
        string code = Code,
        int attempts = 0,
        DateTime? expiresAt = null,
        DateTime? consumedAt = null)
        => new()
        {
            Id = 7,
            PhoneE164 = CanonicalPhone,
            CodeHash = Hasher.Hash(CanonicalPhone, code),
            Purpose = "Login",
            Attempts = attempts,
            MaxAttempts = Options.MaxVerifyAttempts,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = consumedAt,
            CreatedAt = DateTime.UtcNow
        };

    // ── StartAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_NormalizesNumberBeforeSending_WhenGivenLocalFormat()
    {
        var repository = new FakePhoneVerificationRepository();
        var delivery = new FakeOtpDeliveryService();

        var response = await CreateService(repository, delivery)
            .StartAsync(new StartPhoneAuthRequest(RawPhone), requestIp: "1.2.3.4");

        Assert.True(response.Success);
        Assert.Equal(CanonicalPhone, delivery.SentToPhone);
        Assert.Equal(CanonicalPhone, repository.AddedCode!.PhoneE164);
    }

    [Fact]
    public async Task StartAsync_ReturnsFailure_WhenPhoneNumberIsInvalid()
    {
        var delivery = new FakeOtpDeliveryService();

        var response = await CreateService(new FakePhoneVerificationRepository(), delivery)
            .StartAsync(new StartPhoneAuthRequest("12345"), requestIp: null);

        Assert.False(response.Success);
        Assert.Equal("invalid_phone_number", response.ErrorCode);
        Assert.Equal(0, delivery.SendCount);
    }

    /// <summary>
    /// The per-number throttle is what caps the messaging bill; the IP rate limiter cannot
    /// see the number and so cannot stop an attacker rotating addresses against one victim.
    /// </summary>
    [Fact]
    public async Task StartAsync_ReturnsThrottled_WhenSendLimitForNumberIsReached()
    {
        var repository = new FakePhoneVerificationRepository
        {
            CountSendsSinceHandler = (_, _) => Task.FromResult(Options.MaxSendsPerWindow)
        };
        var delivery = new FakeOtpDeliveryService();

        var response = await CreateService(repository, delivery)
            .StartAsync(new StartPhoneAuthRequest(RawPhone), requestIp: null);

        Assert.False(response.Success);
        Assert.Equal(429, response.StatusCode);
        Assert.Equal("otp_send_throttled", response.ErrorCode);
        Assert.Equal(0, delivery.SendCount);
    }

    [Fact]
    public async Task StartAsync_StoresTheCode_WhenDeliveryFails()
    {
        var repository = new FakePhoneVerificationRepository();
        var delivery = new FakeOtpDeliveryService
        {
            SendHandler = (_, _) => Task.FromResult(OtpDeliveryResult.Fail("provider down"))
        };

        var response = await CreateService(repository, delivery)
            .StartAsync(new StartPhoneAuthRequest(RawPhone), requestIp: null);

        Assert.False(response.Success);
        Assert.Equal("otp_delivery_failed", response.ErrorCode);

        // Recorded anyway, so repeated failures still count against the throttle instead of
        // offering an unlimited retry loop.
        Assert.NotNull(repository.AddedCode);
    }

    [Fact]
    public async Task StartAsync_RetiresPreviousChallenges_WhenIssuingANewCode()
    {
        var repository = new FakePhoneVerificationRepository();

        await CreateService(repository, new FakeOtpDeliveryService())
            .StartAsync(new StartPhoneAuthRequest(RawPhone), requestIp: null);

        Assert.Equal(CanonicalPhone, repository.ExpiredForPhone);
    }

    [Fact]
    public async Task StartAsync_NeverStoresThePlaintextCode()
    {
        var repository = new FakePhoneVerificationRepository();
        var delivery = new FakeOtpDeliveryService();

        await CreateService(repository, delivery)
            .StartAsync(new StartPhoneAuthRequest(RawPhone), requestIp: null);

        Assert.NotEqual(delivery.SentCode, repository.AddedCode!.CodeHash);
        Assert.DoesNotContain(delivery.SentCode!, repository.AddedCode.CodeHash);
    }

    // ── VerifyAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenNoChallengeExists()
    {
        var repository = new FakePhoneVerificationRepository();

        var response = await CreateService(repository, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, Code, null));

        Assert.False(response.Success);
        Assert.Equal("invalid_or_expired_code", response.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenChallengeHasExpired()
    {
        var repository = new FakePhoneVerificationRepository
        {
            GetLatestHandler = (_, _) => Task.FromResult<PhoneVerificationCode?>(
                Challenge(expiresAt: DateTime.UtcNow.AddMinutes(-1)))
        };

        var response = await CreateService(repository, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, Code, null));

        Assert.False(response.Success);
        Assert.Equal("invalid_or_expired_code", response.ErrorCode);
    }

    /// <summary>A consumed code must not work a second time.</summary>
    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenChallengeWasAlreadyConsumed()
    {
        var repository = new FakePhoneVerificationRepository
        {
            GetLatestHandler = (_, _) => Task.FromResult<PhoneVerificationCode?>(
                Challenge(consumedAt: DateTime.UtcNow.AddMinutes(-1)))
        };

        var response = await CreateService(repository, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, Code, null));

        Assert.False(response.Success);
        Assert.Equal("invalid_or_expired_code", response.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenAttemptsAreExhausted()
    {
        var repository = new FakePhoneVerificationRepository
        {
            GetLatestHandler = (_, _) => Task.FromResult<PhoneVerificationCode?>(
                Challenge(attempts: Options.MaxVerifyAttempts))
        };

        var response = await CreateService(repository, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, Code, null));

        Assert.False(response.Success);
        Assert.Equal("invalid_or_expired_code", response.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_IncrementsAttempts_WhenCodeIsWrong()
    {
        var repository = new FakePhoneVerificationRepository
        {
            GetLatestHandler = (_, _) => Task.FromResult<PhoneVerificationCode?>(Challenge())
        };

        var response = await CreateService(repository, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, "999999", null));

        Assert.False(response.Success);
        Assert.Equal(7L, repository.IncrementedAttemptsForId);
        Assert.Null(repository.ConsumedId);
    }

    /// <summary>
    /// Every rejection reads the same. Distinguishing "wrong code" from "no challenge" would
    /// tell an attacker which numbers currently have a login in flight.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_ReturnsIdenticalMessage_ForWrongAndMissingChallenge()
    {
        var wrongCode = new FakePhoneVerificationRepository
        {
            GetLatestHandler = (_, _) => Task.FromResult<PhoneVerificationCode?>(Challenge())
        };
        var noChallenge = new FakePhoneVerificationRepository();

        var wrongResponse = await CreateService(wrongCode, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, "999999", null));
        var missingResponse = await CreateService(noChallenge, new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest(RawPhone, Code, null));

        Assert.Equal(missingResponse.Message, wrongResponse.Message);
        Assert.Equal(missingResponse.ErrorCode, wrongResponse.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFailure_WhenPhoneNumberIsInvalid()
    {
        var response = await CreateService(new FakePhoneVerificationRepository(), new FakeOtpDeliveryService())
            .VerifyAsync(new VerifyPhoneAuthRequest("12345", Code, null));

        Assert.False(response.Success);
        Assert.Equal("invalid_phone_number", response.ErrorCode);
    }
}
