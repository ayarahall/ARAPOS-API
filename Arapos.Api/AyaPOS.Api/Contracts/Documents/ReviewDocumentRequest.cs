namespace Ayapos.Api.Contracts.Documents;

public sealed class ReviewDocumentRequest
{
    public Dictionary<string, string?> Fields { get; init; } = new();
}
