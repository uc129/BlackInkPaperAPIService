using System.Net.Http.Json;
using System.Text.Json;

namespace BlackInkPaperAdmin.Services;

public static class ApiResponseReader
{
    //public static async Task<ApiEnvelope<T>> ReadAsync<T>(HttpResponseMessage response)
    //{
    //    if (response.IsSuccessStatusCode)
    //    {
    //        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();
    //        return body ?? new ApiEnvelope<T>
    //        {
    //            Success = false,
    //            StatusCode = (int)response.StatusCode,
    //            Message = "Empty response from API."
    //        };
    //    }

    //    var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
    //    return new ApiEnvelope<T>
    //    {
    //        Success = false,
    //        StatusCode = (int)response.StatusCode,
    //        Message = problem?.Title ?? "Request failed."
    //    };
    //}

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Handles success -> Success
    };
    public static async Task<ApiEnvelope<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        bool isJson = contentType != null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            if (isJson)
            {
                // 1. Read the JSON as 'T' (the flat AuthResponse object)
                // This fills Token, Success, and Message directly into 'T'
                var result = await response.Content.ReadFromJsonAsync<T>(_options);

                if (result != null)
                {
                    // 2. Wrap 'T' into your Envelope manually
                    return new ApiEnvelope<T>
                    {
                        Data = result,
                        Success = true,
                        StatusCode = statusCode,
                        Message = "Success" // Or extract from result using reflection/interface
                    };
                }
                return CreateErrorEnvelope<T>(statusCode, "Empty JSON response.");
            }
            return CreateErrorEnvelope<T>(statusCode, "Success, but response was not JSON.");
        }

        // Handle Failures (ProblemDetails)
        if (isJson)
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(_options);
            return CreateErrorEnvelope<T>(statusCode, problem?.Title ?? "Request failed.");
        }

        var rawContent = await response.Content.ReadAsStringAsync();
        return CreateErrorEnvelope<T>(statusCode, $"Server Error: {rawContent[..Math.Min(rawContent.Length, 100)]}");
    }

    private static ApiEnvelope<T> CreateErrorEnvelope<T>(int statusCode, string message)
        => new(){ Success = false, StatusCode = statusCode, Message = message };
}
