using System.Net.Http.Json;

namespace BlackInkPaperAdmin.Services;

public static class ApiResponseReader
{
    public static async Task<ApiEnvelope<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();
            return body ?? new ApiEnvelope<T>
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Message = "Empty response from API."
            };
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        return new ApiEnvelope<T>
        {
            Success = false,
            StatusCode = (int)response.StatusCode,
            Message = problem?.Title ?? "Request failed."
        };
    }
}
