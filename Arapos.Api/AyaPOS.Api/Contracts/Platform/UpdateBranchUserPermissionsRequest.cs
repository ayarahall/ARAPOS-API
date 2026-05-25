namespace Ayapos.Api.Contracts.Platform;

public sealed class UpdateBranchUserPermissionsRequest
{
    public List<string> Permissions { get; init; } = [];
}
