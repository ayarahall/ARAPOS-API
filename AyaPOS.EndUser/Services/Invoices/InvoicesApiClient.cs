using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Invoices;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Invoices;

public sealed class InvoicesApiClient
{
    private readonly HttpClient _httpClient;

    public InvoicesApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<InvoiceListItemModel>> GetInvoicesAsync(
        SessionState session,
        string? status,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, BuildInvoicesUrl(session.TenantSlug, status, query, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load invoices.", ct);

        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<InvoiceListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<InvoiceListItemModel>();
    }

    public async Task<Guid> CreateDraftAsync(SessionState session, CreateInvoiceRequestModel? model = null, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices");
        request.Content = JsonContent.Create(model ?? new CreateInvoiceRequestModel());

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create invoice.", ct);

        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task<InvoiceDetailsModel> GetInvoiceAsync(SessionState session, Guid invoiceId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/t/{session.TenantSlug}/invoices/{invoiceId}");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load invoice.", ct);

        var payload = await response.Content.ReadFromJsonAsync<InvoiceDetailsModel>(cancellationToken: ct);
        return payload ?? new InvoiceDetailsModel();
    }

    public async Task<Guid> AddItemAsync(SessionState session, Guid invoiceId, AddInvoiceLineRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices/{invoiceId}/items");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not add item to invoice.", ct);

        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task<Guid> SetCustomerAsync(SessionState session, Guid invoiceId, SetInvoiceCustomerRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices/{invoiceId}/customer");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update invoice customer.", ct);

        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task<Guid> UpdateItemAsync(SessionState session, Guid invoiceId, Guid lineId, UpdateInvoiceLineRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices/{invoiceId}/items/{lineId}");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update invoice line.", ct);

        var payload = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        return payload;
    }

    public async Task RemoveItemAsync(SessionState session, Guid invoiceId, Guid lineId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Delete, $"/t/{session.TenantSlug}/invoices/{invoiceId}/items/{lineId}");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not remove invoice line.", ct);
    }

    public async Task<string> FinalizeAsync(SessionState session, Guid invoiceId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices/{invoiceId}/finalize");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not finalize invoice.", ct);

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<InvoiceDetailsModel> AddPaymentAsync(SessionState session, Guid invoiceId, AddPaymentRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/invoices/{invoiceId}/payments");
        request.Content = JsonContent.Create(model);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not record payment.", ct);

        return await GetInvoiceAsync(session, invoiceId, ct);
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

    private static string BuildInvoicesUrl(string tenantSlug, string? status, string? query, int page, int pageSize)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(status))
            parameters["status"] = status.Trim();

        if (!string.IsNullOrWhiteSpace(query))
            parameters["q"] = query.Trim();

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/invoices", parameters);
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
