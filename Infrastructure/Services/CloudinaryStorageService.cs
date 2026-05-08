using Application.DTOs.Storage;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class CloudinaryStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryStorageService(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;
        var account = new Account(
            _options.CloudName,
            _options.ApiKey,
            _options.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public StorageSignatureResponse GenerateUploadSignature(IDictionary<string, object> parameters)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (!parameters.ContainsKey("timestamp"))
            parameters["timestamp"] = timestamp;
        else
            timestamp = Convert.ToInt64(parameters["timestamp"]);

        var signature = _cloudinary.Api.SignParameters(parameters);

        return new StorageSignatureResponse(
            Signature: signature,
            Timestamp: timestamp,
            ApiKey: _options.ApiKey,
            CloudName: _options.CloudName);
    }

    public async Task<bool> DeleteAsync(string publicId, CancellationToken ct = default)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase);
    }
}
