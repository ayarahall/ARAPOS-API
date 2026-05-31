using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Cashier;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Cashier;

public sealed class CashierApiClient
{
    private readonly HttpClient _httpClient;

    public CashierApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<CashierSessionModel?> GetCurrentSessionAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/t/{session.TenantSlug}/cashier/current");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load the current cashier session.", ct);
        return await response.Content.ReadFromJsonAsync<CashierSessionModel>(cancellationToken: ct);
    }

    public async Task<PagedResultModel<CashierSessionModel>> GetSessionsAsync(SessionState session, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"/t/{session.TenantSlug}/cashier/sessions", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        });

        using var request = CreateRequest(session, HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load cashier sessions.", ct);
        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<CashierSessionModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<CashierSessionModel>();
    }

    public async Task<CashierDailySummaryModel> GetDailySummaryAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/t/{session.TenantSlug}/cashier/daily-summary");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load the daily cashier summary.", ct);
        return (await response.Content.ReadFromJsonAsync<CashierDailySummaryModel>(cancellationToken: ct)) ?? new CashierDailySummaryModel();
    }

    public async Task<Guid> OpenSessionAsync(SessionState session, OpenCashierSessionRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/cashier/open");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not open the cashier session.", ct);
        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task<CashierSessionModel> CloseSessionAsync(SessionState session, Guid sessionId, CloseCashierSessionRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/cashier/{sessionId}/close");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not close the cashier session.", ct);
        return (await response.Content.ReadFromJsonAsync<CashierSessionModel>(cancellationToken: ct))!;
    }

    private HttpRequestMessage CreateRequest(SessionState session, HttpMethod method, string url)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("No active sign-in session was found.");

        if (string.IsNullOrWhiteSpace(session.BranchId))
            throw new InvalidOperationException("A branch must be selected first.");

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
}
