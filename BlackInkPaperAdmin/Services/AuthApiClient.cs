using System.Net.Http.Json;
using Application.DTOs.UserAuth;
using Microsoft.Extensions.Options;

namespace BlackInkPaperAdmin.Services;

public class AuthApiClient(HttpClient httpClient, IOptions<AdminApiOptions> optionsAccessor)
{
    private readonly string baseUrl = Normalize(optionsAccessor.Value.BaseUrl);

    public async Task<ApiEnvelope<AuthResponse>> LoginAsync(LoginRequest request)
    {
        using var response = await httpClient.PostAsJsonAsync($"{baseUrl}api/accounts/login", request);
        return await ApiResponseReader.ReadAsync<AuthResponse>(response);
    }

    private static string Normalize(string url) => url.EndsWith('/') ? url : $"{url}/";
}
