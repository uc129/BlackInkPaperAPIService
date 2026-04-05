using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlackInkPaperAdmin.Services;

public static class ApiResponseReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        AllowTrailingCommas = true
    };

    public static async Task<ApiEnvelope<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        bool isJson = contentType != null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            if (!isJson) return CreateErrorEnvelope<T>(statusCode, "Success, but response was not JSON.");

            try
            {
                var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(Options);
                if (envelope != null)
                {
                    // Ensure statusCode is set from HTTP if not in JSON
                    if (envelope.StatusCode == 0) envelope.StatusCode = statusCode;
                    return envelope;
                }
            }
            catch (JsonException ex)
            {
                return CreateErrorEnvelope<T>(statusCode, $"JSON Deserialization Error: {ex.Message}");
            }

            return CreateErrorEnvelope<T>(statusCode, "Empty JSON response.");
        }

        // Handle Failures (ProblemDetails)
        if (isJson)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(Options);
                return CreateErrorEnvelope<T>(statusCode, problem?.Title ?? "Request failed.");
            }
            catch
            {
                var raw = await response.Content.ReadAsStringAsync();
                return CreateErrorEnvelope<T>(statusCode,
                    $"Request failed and could not parse error JSON: {raw[..Math.Min(raw.Length, 100)]}");
            }
        }

        var rawContent = await response.Content.ReadAsStringAsync();
        return CreateErrorEnvelope<T>(statusCode, $"Server Error: {rawContent[..Math.Min(rawContent.Length, 100)]}");
    }

    private static ApiEnvelope<T> CreateErrorEnvelope<T>(int statusCode, string message)
        => new() { Success = false, StatusCode = statusCode, Message = message };
}