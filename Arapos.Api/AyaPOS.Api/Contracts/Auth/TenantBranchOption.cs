namespace Ayapos.Api.Contracts.Auth;

public sealed class TenantBranchOption
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string CurrencyCode { get; init; }
}
