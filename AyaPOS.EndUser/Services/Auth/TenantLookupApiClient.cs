using System.Net.Http.Json;
using Ayapos.EndUser.Models.Auth;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Auth;

public sealed class TenantLookupApiClient
{
    private readonly HttpClient _httpClient;

    public TenantLookupApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<IReadOnlyList<TenantBranchOptionModel>> GetBranchesAsync(string tenantSlug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return [];

        var response = await _httpClient.GetAsync($"/auth/tenant/{tenantSlug.Trim()}/branches", ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "Could not load tenant branches."
                : message);
        }

        var payload = await response.Content.ReadFromJsonAsync<List<TenantBranchOptionModel>>(cancellationToken: ct);
        return payload ?? [];
    }
}
