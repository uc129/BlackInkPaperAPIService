using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserAuth;

public record StartPhoneAuthRequest(
    [Required][MaxLength(20)] string PhoneNumber);

/// <summary>
/// Deliberately says nothing about whether the number belongs to an existing account —
/// the response is identical for a new and a returning customer, so the endpoint cannot be
/// used to enumerate who has bought art here.
/// </summary>
public record StartPhoneAuthResponse(
    string Channel,
    int ExpiresInSeconds);

public record VerifyPhoneAuthRequest(
    [Required][MaxLength(20)] string PhoneNumber,
    [Required][MaxLength(8)] string Code,
    [MaxLength(100)] string? FullName);

public record LinkEmailRequest(
    [Required][EmailAddress][MaxLength(256)] string Email);
