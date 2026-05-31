using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Customers;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Customers;

public sealed class CustomersApiClient
{
    private readonly HttpClient _httpClient;

    public CustomersApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<CustomerListItemModel>> GetCustomersAsync(
        SessionState session,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, BuildCustomersUrl(session.TenantSlug, query, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "فشل تحميل العملاء.", ct);

        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<CustomerListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<CustomerListItemModel>();
    }

    public async Task<Guid> CreateCustomerAsync(SessionState session, CreateCustomerRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/customers");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "فشل إنشاء العميل.", ct);

        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task<CustomerListItemModel> UpdateCustomerAsync(SessionState session, Guid customerId, UpdateCustomerRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/customers/{customerId}");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update customer.", ct);

        return (await response.Content.ReadFromJsonAsync<CustomerListItemModel>(cancellationToken: ct))!;
    }

    private HttpRequestMessage CreateRequest(SessionState session, HttpMethod method, string url)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("لا توجد جلسة دخول فعالة.");

        if (string.IsNullOrWhiteSpace(session.BranchId))
            throw new InvalidOperationException("يجب تحديد الفرع أولاً.");

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        request.Headers.Add("X-Branch-Id", session.BranchId);
        return request;
    }

    private static string BuildCustomersUrl(string tenantSlug, string? query, int page, int pageSize)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(query))
            parameters["q"] = query.Trim();

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/customers", parameters);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? fallbackMessage
            : message);
    }
}
