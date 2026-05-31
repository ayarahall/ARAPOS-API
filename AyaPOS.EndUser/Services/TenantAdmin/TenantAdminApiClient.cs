using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ayapos.EndUser.Models.Owner;
using Ayapos.EndUser.Models.Products;
using Ayapos.EndUser.Models.Staff;
using Ayapos.EndUser.Models.Services;
using Ayapos.EndUser.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Ayapos.EndUser.Services.TenantAdmin;

public sealed class TenantAdminApiClient
{
    private readonly HttpClient _httpClient;

    public TenantAdminApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5167";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PlatformTenantModel> GetTenantAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, "/tenant-admin/tenant");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load tenant summary.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformTenantModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<PlatformBranchModel>> GetBranchesAsync(SessionState session, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, "/tenant-admin/branches");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load tenant branches.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformBranchModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformBranchModel> CreateBranchAsync(SessionState session, CreateBranchRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, "/tenant-admin/branches");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformBranchModel> UpdateBranchAsync(SessionState session, Guid branchId, UpdateBranchRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchModel>(cancellationToken: ct))!;
    }

    public async Task<BranchInvoicePrintSettingsModel> GetBranchPrintSettingsAsync(SessionState session, Guid branchId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/print-settings");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch print settings.", ct);
        return (await response.Content.ReadFromJsonAsync<BranchInvoicePrintSettingsModel>(cancellationToken: ct)) ?? new BranchInvoicePrintSettingsModel();
    }

    public async Task<BranchInvoicePrintSettingsModel> UpdateBranchPrintSettingsAsync(
        SessionState session,
        Guid branchId,
        UpdateBranchInvoicePrintSettingsRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/print-settings");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch print settings.", ct);
        return (await response.Content.ReadFromJsonAsync<BranchInvoicePrintSettingsModel>(cancellationToken: ct)) ?? new BranchInvoicePrintSettingsModel();
    }

    public async Task<IReadOnlyList<PlatformBranchUserModel>> GetBranchUsersAsync(SessionState session, Guid branchId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/users");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch users.", ct);
        return await response.Content.ReadFromJsonAsync<List<PlatformBranchUserModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<PlatformBranchUserModel> CreateBranchUserAsync(SessionState session, Guid branchId, CreateBranchUserRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/users");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch user.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchUserModel>(cancellationToken: ct))!;
    }

    public async Task<PlatformBranchUserModel> UpdateBranchUserLicenseAsync(SessionState session, Guid branchId, Guid userId, UpdateTenantUserLicenseRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/users/{userId}/license");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch user license.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchUserModel>(cancellationToken: ct))!;
    }

    public async Task SetBranchUserPasswordAsync(SessionState session, Guid branchId, Guid userId, SetUserPasswordRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/users/{userId}/password");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not reset branch user password.", ct);
    }

    public async Task<PlatformBranchUserModel> UpdateBranchUserPermissionsAsync(
        SessionState session,
        Guid branchId,
        Guid userId,
        UpdateBranchUserPermissionsRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/users/{userId}/permissions");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch user permissions.", ct);
        return (await response.Content.ReadFromJsonAsync<PlatformBranchUserModel>(cancellationToken: ct))!;
    }

    public async Task<PagedResultModel<ProductListItemModel>> GetBranchProductsAsync(
        SessionState session,
        Guid branchId,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Get, BuildProductsUrl(session.TenantSlug, query, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch products.", ct);
        return (await response.Content.ReadFromJsonAsync<PagedResultModel<ProductListItemModel>>(cancellationToken: ct))
            ?? new PagedResultModel<ProductListItemModel>();
    }

    public async Task<ProductListItemModel> CreateBranchProductAsync(
        SessionState session,
        Guid branchId,
        CreateProductRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Post, $"/t/{session.TenantSlug}/products");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch product.", ct);
        return (await response.Content.ReadFromJsonAsync<ProductListItemModel>(cancellationToken: ct))!;
    }

    public async Task<ProductListItemModel> UpdateBranchProductAsync(
        SessionState session,
        Guid branchId,
        Guid productId,
        UpdateProductRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Post, $"/t/{session.TenantSlug}/products/{productId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch product.", ct);
        return (await response.Content.ReadFromJsonAsync<ProductListItemModel>(cancellationToken: ct))!;
    }

    public async Task<PagedResultModel<ServiceListItemModel>> GetBranchServicesAsync(
        SessionState session,
        Guid branchId,
        string? query,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Get, BuildServicesUrl(session.TenantSlug, query, page, pageSize));
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch services.", ct);
        return (await response.Content.ReadFromJsonAsync<PagedResultModel<ServiceListItemModel>>(cancellationToken: ct))
            ?? new PagedResultModel<ServiceListItemModel>();
    }

    public async Task<ServiceListItemModel> CreateBranchServiceAsync(
        SessionState session,
        Guid branchId,
        CreateServiceRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Post, $"/t/{session.TenantSlug}/services");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create branch service.", ct);
        return (await response.Content.ReadFromJsonAsync<ServiceListItemModel>(cancellationToken: ct))!;
    }

    public async Task<ServiceListItemModel> UpdateBranchServiceAsync(
        SessionState session,
        Guid branchId,
        Guid serviceId,
        UpdateServiceRequestModel model,
        CancellationToken ct = default)
    {
        using var request = CreateBranchScopedRequest(session, branchId, HttpMethod.Post, $"/t/{session.TenantSlug}/services/{serviceId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update branch service.", ct);
        return (await response.Content.ReadFromJsonAsync<ServiceListItemModel>(cancellationToken: ct))!;
    }

    public async Task<ServiceImportResultModel> ImportBranchServicesAsync(
        SessionState session,
        Guid branchId,
        byte[] fileBytes,
        string fileName,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/services/import");
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);
        var extension = Path.GetExtension(fileName);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not import branch services from Excel.", ct);
        return (await response.Content.ReadFromJsonAsync<ServiceImportResultModel>(cancellationToken: ct)) ?? new ServiceImportResultModel();
    }

    public async Task<IReadOnlyList<EmployeeListItemModel>> GetBranchEmployeesAsync(SessionState session, Guid branchId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/employees");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load branch employees.", ct);
        return await response.Content.ReadFromJsonAsync<List<EmployeeListItemModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<BranchAttendanceSummaryModel> GetBranchAttendanceSummaryAsync(SessionState session, Guid branchId, DateTime? date = null, CancellationToken ct = default)
    {
        var url = date.HasValue
            ? QueryHelpers.AddQueryString($"/tenant-admin/branches/{branchId}/employees/attendance-summary", new Dictionary<string, string?> { ["date"] = date.Value.ToString("yyyy-MM-dd") })
            : $"/tenant-admin/branches/{branchId}/employees/attendance-summary";
        using var request = CreateRequest(session, HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load attendance summary.", ct);
        return (await response.Content.ReadFromJsonAsync<BranchAttendanceSummaryModel>(cancellationToken: ct)) ?? new BranchAttendanceSummaryModel();
    }

    public async Task<EmployeeListItemModel> CreateBranchEmployeeAsync(SessionState session, Guid branchId, CreateEmployeeRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create employee.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeListItemModel>(cancellationToken: ct))!;
    }

    public async Task<EmployeeListItemModel> UpdateBranchEmployeeAsync(SessionState session, Guid branchId, Guid employeeId, UpdateEmployeeRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not update employee.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeListItemModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<EmployeeAttendanceModel>> GetEmployeeAttendanceAsync(SessionState session, Guid branchId, Guid employeeId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/attendance");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load employee attendance.", ct);
        return await response.Content.ReadFromJsonAsync<List<EmployeeAttendanceModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<EmployeeAttendanceModel> CheckInEmployeeAsync(SessionState session, Guid branchId, Guid employeeId, EmployeeAttendanceCheckInRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/attendance/check-in");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not check in employee.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeAttendanceModel>(cancellationToken: ct))!;
    }

    public async Task<EmployeeAttendanceModel> CheckOutEmployeeAsync(SessionState session, Guid branchId, Guid employeeId, EmployeeAttendanceCheckOutRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/attendance/check-out");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not check out employee.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeAttendanceModel>(cancellationToken: ct))!;
    }

    public async Task<EmployeeAttendanceModel> MarkEmployeeAttendanceAsync(SessionState session, Guid branchId, Guid employeeId, MarkEmployeeAttendanceRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/attendance/mark");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not mark employee attendance.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeAttendanceModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<EmployeeShiftModel>> GetEmployeeShiftsAsync(SessionState session, Guid branchId, Guid employeeId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/shifts");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load employee shifts.", ct);
        return await response.Content.ReadFromJsonAsync<List<EmployeeShiftModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<EmployeeShiftModel> CreateEmployeeShiftAsync(SessionState session, Guid branchId, Guid employeeId, CreateEmployeeShiftRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/shifts");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create employee shift.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeShiftModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<EmployeeLeaveModel>> GetEmployeeLeavesAsync(SessionState session, Guid branchId, Guid employeeId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/leaves");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load employee leaves.", ct);
        return await response.Content.ReadFromJsonAsync<List<EmployeeLeaveModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<EmployeeLeaveModel> CreateEmployeeLeaveAsync(SessionState session, Guid branchId, Guid employeeId, CreateEmployeeLeaveRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/leaves");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create employee leave.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeLeaveModel>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<EmployeeDocumentModel>> GetEmployeeDocumentsAsync(SessionState session, Guid branchId, Guid employeeId, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Get, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/documents");
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not load employee documents.", ct);
        return await response.Content.ReadFromJsonAsync<List<EmployeeDocumentModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<EmployeeDocumentModel> CreateEmployeeDocumentAsync(SessionState session, Guid branchId, Guid employeeId, CreateEmployeeDocumentRequestModel model, CancellationToken ct = default)
    {
        using var request = CreateRequest(session, HttpMethod.Post, $"/tenant-admin/branches/{branchId}/employees/{employeeId}/documents");
        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Could not create employee document.", ct);
        return (await response.Content.ReadFromJsonAsync<EmployeeDocumentModel>(cancellationToken: ct))!;
    }

    private static HttpRequestMessage CreateRequest(SessionState session, HttpMethod method, string url)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("Tenant sign-in is required first.");

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return request;
    }

    private static HttpRequestMessage CreateBranchScopedRequest(SessionState session, Guid branchId, HttpMethod method, string url)
    {
        var request = CreateRequest(session, method, url);
        request.Headers.Add("X-Branch-Id", branchId.ToString());
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
