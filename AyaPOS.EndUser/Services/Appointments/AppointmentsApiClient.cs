using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Appointments;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Appointments;

public sealed class AppointmentsApiClient
{
    private readonly HttpClient _httpClient;

    public AppointmentsApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PagedResultModel<AppointmentListItemModel>> GetAppointmentsAsync(
        SessionState session,
        string? status,
        string? query,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, BuildAppointmentsUrl(session.TenantSlug, status, query, dateFrom, dateTo, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load appointments.", ct);

        var payload = await response.Content.ReadFromJsonAsync<PagedResultModel<AppointmentListItemModel>>(cancellationToken: ct);
        return payload ?? new PagedResultModel<AppointmentListItemModel>();
    }

    public async Task<IReadOnlyList<AppointmentResourceModel>> GetResourcesAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/t/{session.TenantSlug}/appointments/resources");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load appointment resources.", ct);

        var payload = await response.Content.ReadFromJsonAsync<List<AppointmentResourceModel>>(cancellationToken: ct);
        return payload ?? [];
    }

    public async Task<AppointmentScheduleBoardModel> GetScheduleAsync(SessionState session, DateTime? date = null, CancellationToken ct = default)
    {
        var url = date.HasValue
            ? QueryHelpers.AddQueryString($"/t/{session.TenantSlug}/appointments/schedule", new Dictionary<string, string?> { ["date"] = date.Value.ToString("O") })
            : $"/t/{session.TenantSlug}/appointments/schedule";

        using var request = CreateRequest(session, HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load appointment schedule.", ct);

        return await response.Content.ReadFromJsonAsync<AppointmentScheduleBoardModel>(cancellationToken: ct)
            ?? new AppointmentScheduleBoardModel();
    }

    public async Task<Guid> CreateAppointmentAsync(SessionState session, CreateAppointmentRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/appointments");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create appointment.", ct);
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task<AppointmentListItemModel> UpdateAppointmentAsync(
        SessionState session,
        Guid appointmentId,
        UpdateAppointmentRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/appointments/{appointmentId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update appointment.", ct);
        return (await response.Content.ReadFromJsonAsync<AppointmentListItemModel>(cancellationToken: ct))!;
    }

    public async Task<AppointmentListItemModel> UpdateStatusAsync(
        SessionState session,
        Guid appointmentId,
        UpdateAppointmentStatusRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/t/{session.TenantSlug}/appointments/{appointmentId}/status");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update appointment status.", ct);
        return (await response.Content.ReadFromJsonAsync<AppointmentListItemModel>(cancellationToken: ct))!;
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

    private static string BuildAppointmentsUrl(string tenantSlug, string? status, string? query, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
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

        if (dateFrom.HasValue)
            parameters["dateFrom"] = dateFrom.Value.ToString("O");

        if (dateTo.HasValue)
            parameters["dateTo"] = dateTo.Value.ToString("O");

        return QueryHelpers.AddQueryString($"/t/{tenantSlug}/appointments", parameters);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }
}
