using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Models.Services;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Services;

public sealed class ServicesApiClient
{
    private readonly HttpClient _httpClient;

    public ServicesApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<ServiceListItemModel>> GetServicesAsync(
        SessionState session,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("لا توجد جلسة دخول فعالة.");

        if (string.IsNullOrWhiteSpace(session.BranchId))
            throw new InvalidOperationException("يجب تحديد الفرع أولاً.");

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildServicesUrl(session.TenantSlug, query, page, pageSize));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        request.Headers.Add("X-Branch-Id", session.BranchId);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "فشل تحميل الخدمات."
                : message);
        }

        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<ServiceListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<ServiceListItemModel>();
    }

    public async Task<ServiceListItemModel> CreateServiceAsync(
        SessionState session,
        CreateServiceRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/services");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create service.", ct);
        return (await response.Content.ReadFromJsonAsync<ServiceListItemModel>(cancellationToken: ct))!;
    }

    public async Task<ServiceListItemModel> UpdateServiceAsync(
        SessionState session,
        Guid serviceId,
        UpdateServiceRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/services/{serviceId}");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update service.", ct);
        return (await response.Content.ReadFromJsonAsync<ServiceListItemModel>(cancellationToken: ct))!;
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

    private static string BuildServicesUrl(string tenantSlug, string? query, int page, int pageSize)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(query))
            parameters["q"] = query.Trim();

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/services", parameters);
    }
}
