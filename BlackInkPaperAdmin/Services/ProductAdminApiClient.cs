using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Products;
using Microsoft.Extensions.Options;

namespace BlackInkPaperAdmin.Services;

public class ProductAdminApiClient(HttpClient httpClient, IOptions<AdminApiOptions> optionsAccessor)
{
    private readonly string baseUrl = Normalize(optionsAccessor.Value.BaseUrl);

    public Task<ApiEnvelope<PagedResultDto<ProductSummaryDto>>> SearchAsync(AdminSession session, ProductSearchRequest request)
        => SendAsync<PagedResultDto<ProductSummaryDto>>(session, HttpMethod.Get, BuildSearchUrl(request));

    public Task<ApiEnvelope<ProductResponseDto>> GetByIdAsync(AdminSession session, int id)
        => SendAsync<ProductResponseDto>(session, HttpMethod.Get, $"api/admin/products/{id}");

    public Task<ApiEnvelope<ProductResponseDto>> CreateAsync(AdminSession session, CreateProductRequest request)
        => SendAsync<ProductResponseDto>(session, HttpMethod.Post, "api/admin/products", request);

    public Task<ApiEnvelope<ProductResponseDto>> UpdateAsync(AdminSession session, int id, UpdateProductRequest request)
        => SendAsync<ProductResponseDto>(session, HttpMethod.Put, $"api/admin/products/{id}", request);

    public Task<ApiEnvelope<bool>> DeleteAsync(AdminSession session, int id)
        => SendAsync<bool>(session, HttpMethod.Delete, $"api/admin/products/{id}");

    private async Task<ApiEnvelope<T>> SendAsync<T>(AdminSession session, HttpMethod method, string relativeUrl, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, $"{baseUrl}{relativeUrl}");
        if (session.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await httpClient.SendAsync(request);
        return await ApiResponseReader.ReadAsync<T>(response);
    }

    private static string BuildSearchUrl(ProductSearchRequest request)
    {
        var query = new List<string>
        {
            $"Page={request.Page}",
            $"PageSize={request.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query.Add($"Query={Uri.EscapeDataString(request.Query)}");
        }

        return $"api/admin/products?{string.Join("&", query)}";
    }

    private static string Normalize(string url) => url.EndsWith('/') ? url : $"{url}/";
}
