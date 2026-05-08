using Application.DTOs.Storage;

namespace Infrastructure.Contracts.Services;

public interface IStorageService
{
    StorageSignatureResponse GenerateUploadSignature(IDictionary<string, object> parameters);
    Task<bool> DeleteAsync(string publicId, CancellationToken ct = default);
}
