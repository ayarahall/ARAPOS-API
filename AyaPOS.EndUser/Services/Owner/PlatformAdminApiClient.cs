using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Auth;
using Ayapos.EndUser.Models.Owner;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.Owner;

public sealed class PlatformAdminApiClient
{
    private readonly HttpClient _httpClient;

    public PlatformAdminApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<LoginApiResponse> LoginAsync(OwnerLoginFormModel model, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/platform/login", new OwnerLoginRequestModel
        {
            Username = model.Username.Trim(),
            Password = model.Password
        }, ct);

        await EnsureSuccessAsync(response, "Could not sign in as owner.", ct);
        return (await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<PlatformTenantModel>> GetTenantsAsync(OwnerSessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, "/platform/tenants");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load tenants.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformTenantModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<IReadOnlyList<PlatformOwnerModel>> GetOwnersAsync(OwnerSessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, "/platform/owners");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load platform owners.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformOwnerModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformOwnerModel> CreateOwnerAsync(OwnerSessionState session, CreatePlatformOwnerRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, "/platform/owners");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create platform owner.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformOwnerModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformOwnerModel> UpdateOwnerStatusAsync(OwnerSessionState session, Guid ownerId, UpdatePlatformOwnerStatusRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/owners/{ownerId}/status");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update owner status.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformOwnerModel>(cancellationToken: ct))!;
    }

    public async Task SetOwnerPasswordAsync(OwnerSessionState session, Guid ownerId, SetPlatformOwnerPasswordRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/owners/{ownerId}/password");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not reset owner password.", ct);
    }

    public async Task<PlatformTenantModel> CreateTenantAsync(OwnerSessionState session, CreatePlatformTenantRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, "/platform/tenants");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create tenant.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformTenantModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformTenantModel> UpdateTenantLicenseAsync(OwnerSessionState session, Guid tenantId, UpdateTenantLicenseRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/license");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update tenant license.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformTenantModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<PlatformBranchModel>> GetBranchesAsync(OwnerSessionState session, Guid tenantId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/platform/tenants/{tenantId}/branches");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branches.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformBranchModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformBranchModel> CreateBranchAsync(OwnerSessionState session, Guid tenantId, CreateBranchRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/branches");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformBranchModel> UpdateBranchAsync(OwnerSessionState session, Guid tenantId, Guid branchId, UpdateBranchRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/branches/{branchId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<PlatformTenantUserModel>> GetTenantUsersAsync(OwnerSessionState session, Guid tenantId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/platform/tenants/{tenantId}/users");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load tenant users.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformTenantUserModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformTenantUserModel> CreateTenantUserAsync(OwnerSessionState session, Guid tenantId, CreateTenantUserRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/users");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create tenant user.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformTenantUserModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformTenantUserModel> UpdateTenantUserLicenseAsync(OwnerSessionState session, Guid tenantId, Guid userId, UpdateTenantUserLicenseRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/users/{userId}/license");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update tenant user license.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformTenantUserModel>(cancellationToken: ct))!;
    }

    public async Task SetTenantUserPasswordAsync(OwnerSessionState session, Guid tenantId, Guid userId, SetUserPasswordRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/users/{userId}/password");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not reset tenant user password.", ct);
    }

    public async Task<IReadOnlyList<PlatformBranchUserModel>> GetBranchUsersAsync(OwnerSessionState session, Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/platform/tenants/{tenantId}/branches/{branchId}/users");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch users.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformBranchUserModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformBranchUserModel> CreateBranchUserAsync(OwnerSessionState session, Guid tenantId, Guid branchId, CreateBranchUserRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/branches/{branchId}/users");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch user.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchUserModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformBranchUserModel> UpdateBranchUserLicenseAsync(OwnerSessionState session, Guid tenantId, Guid branchId, Guid userId, UpdateTenantUserLicenseRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/branches/{branchId}/users/{userId}/license");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch user license.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchUserModel>(cancellationToken: ct))!;
    }

    public async Task SetBranchUserPasswordAsync(OwnerSessionState session, Guid tenantId, Guid branchId, Guid userId, SetUserPasswordRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/platform/tenants/{tenantId}/branches/{branchId}/users/{userId}/password");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not reset branch user password.", ct);
    }

    private static HttpRequestMessage CreateRequest(OwnerSessionState session, HttpMethod method, string url)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("Owner sign-in is required first.");

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
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
