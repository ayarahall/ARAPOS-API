using System.Net.Http.Json;
using Ayapos.EndUser.Models.Auth;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Auth;

public sealed class AuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<LoginApiResponse> LoginAsync(LoginFormModel model, CancellationToken ct = default)
    {
        var branchId = Guid.TryParse(model.BranchId, out var parsedBranchId)
            ? parsedBranchId
            : (Guid?)null;

        var response = await _httpClient.PostAsJsonAsync("/auth/login", new LoginApiRequest
        {
            TenantSlug = model.TenantSlug.Trim(),
            BranchId = branchId,
            Username = model.Username.Trim(),
            Password = model.Password
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "Sign in failed."
                : message);
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
            throw new InvalidOperationException("The server did not return a valid sign-in token.");

        return payload;
    }
}
