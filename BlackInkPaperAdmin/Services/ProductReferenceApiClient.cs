using System.Net.Http.Headers;
using Application.DTOs.Products;
using Microsoft.Extensions.Options;

namespace BlackInkPaperAdmin.Services;

public class ProductReferenceApiClient(HttpClient httpClient, IOptions<AdminApiOptions> optionsAccessor)
{
    private readonly string _baseUrl = Normalize(optionsAccessor.Value.BaseUrl);

    public Task<ApiEnvelope<IReadOnlyList<ArtistLookupDto>>> GetArtistsAsync(AdminSession session)
        => SendAsync<IReadOnlyList<ArtistLookupDto>>(session, HttpMethod.Get, "api/admin/product-options/artists");

    public Task<ApiEnvelope<IReadOnlyList<ProductCategoryLookupDto>>> GetCategoriesAsync(AdminSession session)
        => SendAsync<IReadOnlyList<ProductCategoryLookupDto>>(session, HttpMethod.Get, "api/admin/product-options/categories");

    public Task<ApiEnvelope<IReadOnlyList<ProductSubCategoryLookupDto>>> GetSubCategoriesAsync(AdminSession session, int? categoryId)
        => SendAsync<IReadOnlyList<ProductSubCategoryLookupDto>>(session, HttpMethod.Get, categoryId is > 0
            ? $"api/admin/product-options/subcategories?categoryId={categoryId}"
            : "api/admin/product-options/subcategories");

    public Task<ApiEnvelope<IReadOnlyList<ProductTagDto>>> GetTagsAsync(AdminSession session)
        => SendAsync<IReadOnlyList<ProductTagDto>>(session, HttpMethod.Get, "api/admin/product-options/tags");

    public Task<ApiEnvelope<ProductCategoryLookupDto>> CreateCategoryAsync(AdminSession session, CreateProductCategoryRequest request)
        => SendAsync<ProductCategoryLookupDto>(session, HttpMethod.Post, "api/admin/product-options/categories", request);

    public Task<ApiEnvelope<ProductSubCategoryLookupDto>> CreateSubCategoryAsync(AdminSession session, CreateProductSubCategoryRequest request)
        => SendAsync<ProductSubCategoryLookupDto>(session, HttpMethod.Post, "api/admin/product-options/subcategories", request);

    private async Task<ApiEnvelope<T>> SendAsync<T>(AdminSession session, HttpMethod method, string relativeUrl, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
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

    private static string Normalize(string url) => url.EndsWith('/') ? url : $"{url}/";
}
