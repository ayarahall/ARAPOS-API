using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Products;

public sealed class ProductsApiClient
{
    private readonly HttpClient _httpClient;

    public ProductsApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<ProductListItemModel>> GetProductsAsync(
        SessionState session,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("No active sign-in session was found.");

        if (string.IsNullOrWhiteSpace(session.BranchId))
            throw new InvalidOperationException("A branch must be selected first.");

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildProductsUrl(session.TenantSlug, query, page, pageSize));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        request.Headers.Add("X-Branch-Id", session.BranchId);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "Failed loading products."
                : message);
        }

        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<ProductListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<ProductListItemModel>();
    }

    public async Task<ProductListItemModel> CreateProductAsync(
        SessionState session,
        CreateProductRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/products");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create product.", ct);
        return (await response.Content.ReadFromJsonAsync<ProductListItemModel>(cancellationToken: ct))!;
    }

    public async Task<ProductListItemModel> UpdateProductAsync(
        SessionState session,
        Guid productId,
        UpdateProductRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/products/{productId}");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update product.", ct);
        return (await response.Content.ReadFromJsonAsync<ProductListItemModel>(cancellationToken: ct))!;
    }

    private static HttpRequestMessage CreateBranchRequest(SessionState session, HttpMethod method, string url)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("An active sign-in session is required first.");

        if (string.IsNullOrWhiteSpace(session.BranchId))
            throw new InvalidOperationException("Branch Id must be selected first.");

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        request.Headers.Add("X-Branch-Id", session.BranchId);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }

    private static string BuildProductsUrl(string tenantSlug, string? query, int page, int pageSize)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(query))
            parameters["q"] = query.Trim();

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/products", parameters);
    }
}
