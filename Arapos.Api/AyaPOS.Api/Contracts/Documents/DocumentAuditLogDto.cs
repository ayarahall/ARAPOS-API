namespace Ayapos.Api.Contracts.Documents;

public sealed class DocumentAuditLogDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = "";
    public Guid? ActorUserId { get; init; }
    public string? DetailsJson { get; init; }
    public DateTime CreatedAt { get; init; }
}
