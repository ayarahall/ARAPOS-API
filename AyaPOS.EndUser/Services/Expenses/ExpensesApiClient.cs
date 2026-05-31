using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Expenses;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Forms;

namespace Ayapos.EndUser.Services.Expenses;

public sealed class ExpensesApiClient
{
    private readonly HttpClient _httpClient;

    public ExpensesApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<BranchExpenseListItemModel>> GetExpensesAsync(
        SessionState session,
        string? category,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, BuildExpensesUrl(session.TenantSlug, category, query, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load expenses.", ct);
        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<BranchExpenseListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<BranchExpenseListItemModel>();
    }

    public async Task<ExpenseAiStatusModel> GetAiStatusAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/t/{session.TenantSlug}/expenses/ai-status");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load AI expense status.", ct);
        return (await response.Content.ReadFromJsonAsync<ExpenseAiStatusModel>(cancellationToken: ct)) ?? new ExpenseAiStatusModel();
    }

    public async Task<Guid> CreateExpenseAsync(SessionState session, CreateBranchExpenseRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/expenses");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create expense.", ct);
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task<BranchExpenseListItemModel> UpdateStatusAsync(
        SessionState session,
        Guid expenseId,
        UpdateBranchExpenseStatusRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/expenses/{expenseId}/status");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update expense status.", ct);
        return (await response.Content.ReadFromJsonAsync<BranchExpenseListItemModel>(cancellationToken: ct))!;
    }

    public async Task<AnalyzeExpenseReceiptResultModel> AnalyzeReceiptAsync(
        SessionState session,
        IBrowserFile file,
        CancellationToken ct = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(12));

        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/expenses/analyze-receipt");
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream(10_000_000, timeoutCts.Token);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "file", file.Name);
        request.Content = form;

        try
        {
            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            await EnsureSuccessAsync(response, "Could not analyze expense receipt.", timeoutCts.Token);
            return (await response.Content.ReadFromJsonAsync<AnalyzeExpenseReceiptResultModel>(cancellationToken: timeoutCts.Token))!;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("Expense receipt analysis is unavailable right now. Add the OpenAI key or continue manually.");
        }
    }

    private static HttpRequestMessage CreateRequest(SessionState session, HttpMethod method, string url)
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

    private static string BuildExpensesUrl(string tenantSlug, string? category, string? query, int page, int pageSize)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(category))
            parameters["category"] = category.Trim();

        if (!string.IsNullOrWhiteSpace(query))
            parameters["q"] = query.Trim();

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/expenses", parameters);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }
}
