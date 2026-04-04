namespace Arapos.Api.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsPlatform { get; }

    Guid? BranchId { get; set; }   // <-- جديد
}
