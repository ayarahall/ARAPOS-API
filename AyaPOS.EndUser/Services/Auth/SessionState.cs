using Microsoft.JSInterop;

namespace Ayapos.EndUser.Services.Auth;

public sealed class SessionState
{
    private const string StorageKey = "ayapos.enduser.session.v2";

    private readonly IJSRuntime _jsRuntime;

    public SessionState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string Token { get; private set; } = string.Empty;
    public string TenantSlug { get; private set; } = string.Empty;
    public string BranchId { get; private set; } = string.Empty;
    public string BranchName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string TenantId { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Permissions => _permissions;
    public bool PermissionsConfigured { get; private set; }

    private HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
    public bool IsTenantAdmin =>
        string.Equals(Role, "TENANT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    public bool IsCashier =>
        string.Equals(Role, "CASHIER", StringComparison.OrdinalIgnoreCase);
    public bool IsHr =>
        string.Equals(Role, "HR", StringComparison.OrdinalIgnoreCase);
    public bool IsBranchManager =>
        string.Equals(Role, "BRANCH_MANAGER", StringComparison.OrdinalIgnoreCase);
    public bool IsOwner =>
        string.Equals(Role, "OWNER", StringComparison.OrdinalIgnoreCase);
    public bool IsBranchUser => IsCashier || IsHr || IsBranchManager;
    public bool CanAccessPos => HasPermission("pos");
    public bool CanAccessAppointments => HasPermission("appointments");
    public bool CanAccessExpenses => HasPermission("expenses");
    public bool CanAccessEmployees => HasPermission("employees");
    public bool CanAccessCatalog => HasPermission("products");
    public bool CanAccessCustomers => HasPermission("customers");
    public bool CanAccessServices => HasPermission("services");
    public bool CanAccessInvoices => HasPermission("invoices");
    public bool CanAccessCashier => HasPermission("cashier");

    public async Task InitializeAsync()
    {
        var session = await _jsRuntime.InvokeAsync<StoredSession?>("ayaposSession.get", StorageKey);
        if (session is null)
            return;

        Token = session.Token ?? string.Empty;
        TenantSlug = session.TenantSlug ?? string.Empty;
        BranchId = session.BranchId ?? string.Empty;
        BranchName = session.BranchName ?? string.Empty;
        Username = session.Username ?? string.Empty;
        Role = session.Role ?? string.Empty;
        TenantId = session.TenantId ?? string.Empty;
        PermissionsConfigured = session.PermissionsConfigured;
        _permissions = new HashSet<string>(session.Permissions ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public async Task SignInAsync(
        string token,
        string tenantSlug,
        string branchId,
        string branchName,
        string username,
        string role,
        string tenantId,
        IEnumerable<string>? permissions = null,
        bool permissionsConfigured = false)
    {
        Token = token;
        TenantSlug = tenantSlug;
        BranchId = branchId;
        BranchName = branchName;
        Username = username;
        Role = role;
        TenantId = tenantId;
        PermissionsConfigured = permissionsConfigured;
        _permissions = new HashSet<string>(permissions ?? [], StringComparer.OrdinalIgnoreCase);

        await _jsRuntime.InvokeVoidAsync("ayaposSession.set", StorageKey, new StoredSession
        {
            Token = token,
            TenantSlug = tenantSlug,
            BranchId = branchId,
            BranchName = branchName,
            Username = username,
            Role = role,
            TenantId = tenantId,
            Permissions = _permissions.ToList(),
            PermissionsConfigured = permissionsConfigured
        });
    }

    public async Task SignOutAsync()
    {
        Token = string.Empty;
        TenantSlug = string.Empty;
        BranchId = string.Empty;
        BranchName = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
        TenantId = string.Empty;
        PermissionsConfigured = false;
        _permissions.Clear();
        await _jsRuntime.InvokeVoidAsync("ayaposSession.remove", StorageKey);
    }

    public bool HasPermission(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (IsOwner)
            return true;

        if (PermissionsConfigured)
            return _permissions.Contains(key);

        return GetRoleDefaults(Role).Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetRoleDefaults(string? role)
    {
        var normalized = (role ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "OWNER" => ["pos", "products", "services", "customers", "appointments", "employees", "expenses", "cashier", "invoices", "reports"],
            "TENANT" or "ADMIN" => ["pos", "products", "services", "customers", "appointments", "employees", "expenses", "cashier", "invoices", "reports"],
            "BRANCH_MANAGER" => ["pos", "products", "services", "customers", "appointments", "employees", "expenses", "cashier", "invoices", "reports"],
            "HR" => ["customers", "appointments", "employees", "expenses"],
            "CASHIER" => ["pos", "products", "services", "customers", "appointments", "cashier", "invoices"],
            _ => []
        };
    }

    private sealed class StoredSession
    {
        public string? Token { get; set; }
        public string? TenantSlug { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? TenantId { get; set; }
        public List<string>? Permissions { get; set; }
        public bool PermissionsConfigured { get; set; }
    }
}
