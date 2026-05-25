using System.Text.Json;
using Ayapos.Api.Data.Entities;

namespace Ayapos.Api.Security;

public static class UserPermissionCatalog
{
    // ── Feature-level (top-level access gates) ────────────────────────────
    public const string Pos = "pos";
    public const string Products = "products";
    public const string Services = "services";
    public const string Customers = "customers";
    public const string Appointments = "appointments";
    public const string Employees = "employees";
    public const string Expenses = "expenses";
    public const string Cashier = "cashier";
    public const string Invoices = "invoices";
    public const string Reports = "reports";

    // ── POS sub-permissions ───────────────────────────────────────────────
    public const string PosDiscount = "pos.discount";
    public const string PosRefund = "pos.refund";
    public const string PosVoid = "pos.void";

    // ── Products sub-permissions ──────────────────────────────────────────
    public const string ProductsEdit = "products.edit";
    public const string ProductsCreate = "products.create";

    // ── Services sub-permissions ──────────────────────────────────────────
    public const string ServicesEdit = "services.edit";
    public const string ServicesCreate = "services.create";

    // ── Customers sub-permissions ─────────────────────────────────────────
    public const string CustomersEdit = "customers.edit";
    public const string CustomersDelete = "customers.delete";

    // ── Appointments sub-permissions ──────────────────────────────────────
    public const string AppointmentsViewAll = "appointments.view_all";
    public const string AppointmentsCreate = "appointments.create";
    public const string AppointmentsEdit = "appointments.edit";
    public const string AppointmentsCancel = "appointments.cancel";

    // ── Employees sub-permissions ─────────────────────────────────────────
    public const string EmployeesEdit = "employees.edit";
    public const string EmployeesSalary = "employees.salary";
    public const string EmployeesAttendance = "employees.attendance";
    public const string EmployeesCreate = "employees.create";

    // ── Expenses sub-permissions ──────────────────────────────────────────
    public const string ExpensesApprove = "expenses.approve";
    public const string ExpensesCreate = "expenses.create";

    // ── Cashier sub-permissions ───────────────────────────────────────────
    public const string CashierClose = "cashier.close";
    public const string CashierViewHistory = "cashier.history";

    // ── Invoices sub-permissions ──────────────────────────────────────────
    public const string InvoicesEdit = "invoices.edit";
    public const string InvoicesRefund = "invoices.refund";
    public const string InvoicesViewAll = "invoices.view_all";

    // ── Reports sub-permissions ───────────────────────────────────────────
    public const string ReportsFinancial = "reports.financial";
    public const string ReportsExport = "reports.export";
    public const string ReportsStaff = "reports.staff";

    public static readonly IReadOnlyList<string> All =
    [
        Pos, PosDiscount, PosRefund, PosVoid,
        Products, ProductsEdit, ProductsCreate,
        Services, ServicesEdit, ServicesCreate,
        Customers, CustomersEdit, CustomersDelete,
        Appointments, AppointmentsViewAll, AppointmentsCreate, AppointmentsEdit, AppointmentsCancel,
        Employees, EmployeesEdit, EmployeesSalary, EmployeesAttendance, EmployeesCreate,
        Expenses, ExpensesApprove, ExpensesCreate,
        Cashier, CashierClose, CashierViewHistory,
        Invoices, InvoicesEdit, InvoicesRefund, InvoicesViewAll,
        Reports, ReportsFinancial, ReportsExport, ReportsStaff,
    ];

    public static IReadOnlyList<string> GetEffectivePermissions(User user)
    {
        var configured = ParsePermissions(user.PermissionsJson);
        return configured.Count > 0 || !string.IsNullOrWhiteSpace(user.PermissionsJson)
            ? configured
            : GetDefaultPermissionsForRole(user.Role);
    }

    public static bool HasExplicitPermissions(User user)
        => !string.IsNullOrWhiteSpace(user.PermissionsJson);

    public static IReadOnlyList<string> GetDefaultPermissionsForRole(string? role)
    {
        var normalized = (role ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "OWNER" or "TENANT" or "ADMIN" => All,
            "BRANCH_MANAGER" =>
            [
                Pos, PosDiscount, PosRefund,
                Products, ProductsEdit, ProductsCreate,
                Services, ServicesEdit, ServicesCreate,
                Customers, CustomersEdit,
                Appointments, AppointmentsViewAll, AppointmentsCreate, AppointmentsEdit, AppointmentsCancel,
                Employees, EmployeesEdit, EmployeesAttendance,
                Expenses, ExpensesApprove, ExpensesCreate,
                Cashier, CashierClose, CashierViewHistory,
                Invoices, InvoicesEdit, InvoicesViewAll,
                Reports, ReportsFinancial, ReportsStaff,
            ],
            "HR" =>
            [
                Customers,
                Appointments, AppointmentsViewAll, AppointmentsCreate, AppointmentsEdit,
                Employees, EmployeesEdit, EmployeesAttendance,
                Expenses, ExpensesCreate,
            ],
            "CASHIER" =>
            [
                Pos, PosDiscount,
                Products, Services,
                Customers,
                Appointments, AppointmentsCreate,
                Cashier,
                Invoices,
            ],
            _ => []
        };
    }

    public static string SerializePermissions(IEnumerable<string>? permissions)
    {
        var normalized = NormalizePermissions(permissions);
        return JsonSerializer.Serialize(normalized);
    }

    public static IReadOnlyList<string> ParsePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return [];

        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(permissionsJson) ?? [];
            return NormalizePermissions(values);
        }
        catch
        {
            return [];
        }
    }

    public static List<string> NormalizePermissions(IEnumerable<string>? permissions)
    {
        var allowed = new HashSet<string>(All, StringComparer.OrdinalIgnoreCase);
        return (permissions ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Where(value => allowed.Contains(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
    }
}
