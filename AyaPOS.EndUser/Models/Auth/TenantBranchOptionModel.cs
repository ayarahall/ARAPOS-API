namespace Ayapos.EndUser.Models.Auth;

public sealed class TenantBranchOptionModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
