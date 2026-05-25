namespace Ayapos.Api.Security;

public static class AuthPolicies
{
    public const string PlatformOnly = "PlatformOnly";
    public const string PlatformOwner = "PlatformOwner";

    public const string TenantOnly = "TenantOnly";
    public const string TenantOwner = "TenantOwner";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantCashier = "TenantCashier";
}
