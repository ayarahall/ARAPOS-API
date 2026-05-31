namespace Ayapos.EndUser.Models.Owner;

public sealed class CreateBranchRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "AED";
}
