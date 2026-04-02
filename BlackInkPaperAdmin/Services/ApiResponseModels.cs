using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlackInkPaperAdmin.Services;

public class ApiEnvelope<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public class ApiProblemDetails
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
