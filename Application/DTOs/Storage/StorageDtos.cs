namespace Application.DTOs.Storage;

public record StorageSignatureRequest(IDictionary<string, object> Parameters);

public record StorageSignatureResponse(
    string Signature, 
    long Timestamp, 
    string ApiKey, 
    string CloudName);
