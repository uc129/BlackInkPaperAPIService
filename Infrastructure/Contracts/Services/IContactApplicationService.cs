using Application.DTOs.Contact;
using Application.DTOs.Products;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IContactApplicationService
{
    Task<ServiceResponse<object>> SubmitAsync(SubmitContactRequest request, CancellationToken ct = default);
    Task<ServiceResponse<PagedResultDto<ContactSubmissionDto>>> GetSubmissionsAsync(
        ContactSubmissionSearchRequest request, CancellationToken ct = default);
    Task<ServiceResponse<ContactSubmissionDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResponse<object>> ResolveAsync(int id, ResolveContactSubmissionRequest request, CancellationToken ct = default);
}
