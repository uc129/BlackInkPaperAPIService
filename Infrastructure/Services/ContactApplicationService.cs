using Application.DTOs.Contact;
using Application.DTOs.Products;
using Common.YourProject.Models;
using Domain.Aggregates.Contact;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class ContactApplicationService(
    IContactRepository contactRepository,
    IEmailService emailService,
    IOptions<ContactOptions> contactOptions,
    ILogger<ContactApplicationService> logger) : IContactApplicationService
{
    public async Task<ServiceResponse<object>> SubmitAsync(SubmitContactRequest request, CancellationToken ct = default)
    {
        try
        {
            var submission = new ContactSubmissionAggregate
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Subject = request.Subject.Trim(),
                Message = request.Message.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            await contactRepository.AddAsync(submission, ct);

            var adminEmail = contactOptions.Value.AdminNotificationEmail;
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                await emailService.SendAsync(
                    adminEmail,
                    $"New contact message: {submission.Subject}",
                    BuildAdminNotificationHtml(submission),
                    ct);
            }

            await emailService.SendAsync(
                submission.Email,
                "We received your message — Black Ink Paper",
                BuildConfirmationHtml(submission.Name),
                ct);

            return ServiceResponse<object>.Ok(new { }, "Your message has been sent. We'll get back to you soon.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process contact submission from {Email}.", request.Email);
            return ServiceResponse<object>.Fail("Unable to send your message. Please try again later.", ex.ToString(), 500, "contact_submit_failed");
        }
    }

    public async Task<ServiceResponse<PagedResultDto<ContactSubmissionDto>>> GetSubmissionsAsync(
        ContactSubmissionSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var (items, totalCount) = await contactRepository.SearchAsync(request, ct);
            var dtos = items.Select(ToDto).ToList();
            var result = new PagedResultDto<ContactSubmissionDto>(dtos, request.Page, request.PageSize, totalCount);
            return ServiceResponse<PagedResultDto<ContactSubmissionDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch contact submissions.");
            return ServiceResponse<PagedResultDto<ContactSubmissionDto>>.Fail("Unable to fetch submissions.", ex.ToString(), 500, "contact_read_failed");
        }
    }

    public async Task<ServiceResponse<ContactSubmissionDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var submission = await contactRepository.GetByIdAsync(id, ct);
            if (submission is null)
            {
                return ServiceResponse<ContactSubmissionDto>.Fail("Contact submission not found.", statusCode: 404, errorCode: "contact_not_found");
            }

            return ServiceResponse<ContactSubmissionDto>.Ok(ToDto(submission));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch contact submission {Id}.", id);
            return ServiceResponse<ContactSubmissionDto>.Fail("Unable to fetch submission.", ex.ToString(), 500, "contact_read_failed");
        }
    }

    public async Task<ServiceResponse<object>> ResolveAsync(int id, ResolveContactSubmissionRequest request, CancellationToken ct = default)
    {
        try
        {
            var submission = await contactRepository.GetByIdAsync(id, ct);
            if (submission is null)
            {
                return ServiceResponse<object>.Fail("Contact submission not found.", statusCode: 404, errorCode: "contact_not_found");
            }

            if (submission.IsResolved)
            {
                return ServiceResponse<object>.Fail("Submission is already resolved.", statusCode: 400, errorCode: "contact_already_resolved");
            }

            await contactRepository.MarkResolvedAsync(id, request.Notes, DateTime.UtcNow, ct);
            return ServiceResponse<object>.Ok(new { }, "Submission marked as resolved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve contact submission {Id}.", id);
            return ServiceResponse<object>.Fail("Unable to resolve submission.", ex.ToString(), 500, "contact_resolve_failed");
        }
    }

    private static ContactSubmissionDto ToDto(ContactSubmissionAggregate s) => new(
        s.Id, s.Name, s.Email, s.Subject, s.Message,
        s.SubmittedAt, s.IsResolved, s.ResolvedAt, s.ResolvedNotes);

    private static string BuildAdminNotificationHtml(ContactSubmissionAggregate s) => $"""
        <div style="font-family:sans-serif;max-width:600px;">
            <h2>New Contact Form Submission</h2>
            <table style="border-collapse:collapse;width:100%;">
                <tr><td style="padding:8px;font-weight:bold;background:#f5f5f5;">Name</td><td style="padding:8px;">{System.Web.HttpUtility.HtmlEncode(s.Name)}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;background:#f5f5f5;">Email</td><td style="padding:8px;">{System.Web.HttpUtility.HtmlEncode(s.Email)}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;background:#f5f5f5;">Subject</td><td style="padding:8px;">{System.Web.HttpUtility.HtmlEncode(s.Subject)}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;background:#f5f5f5;">Message</td><td style="padding:8px;white-space:pre-wrap;">{System.Web.HttpUtility.HtmlEncode(s.Message)}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;background:#f5f5f5;">Submitted</td><td style="padding:8px;">{s.SubmittedAt:yyyy-MM-dd HH:mm} UTC</td></tr>
            </table>
        </div>
        """;

    private static string BuildConfirmationHtml(string name) => $"""
        <div style="font-family:sans-serif;max-width:600px;">
            <h2>Thank you for reaching out, {System.Web.HttpUtility.HtmlEncode(name)}!</h2>
            <p>We've received your message and will get back to you within 2–3 business days.</p>
            <p>In the meantime, feel free to browse our collection at <a href="https://blackinkpaper.com">blackinkpaper.com</a>.</p>
            <p style="margin-top:32px;color:#666;">— The Black Ink Paper Team</p>
        </div>
        """;
}
