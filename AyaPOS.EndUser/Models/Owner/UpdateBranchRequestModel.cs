namespace Ayapos.EndUser.Models.Owner;

public sealed class UpdateBranchRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "AED";
    public bool IsActive { get; set; } = true;
}
