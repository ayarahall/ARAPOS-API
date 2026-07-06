namespace Ayapos.Api.Services.Documents;

public interface IStructuredFieldExtractor
{
    /// <summary>
    /// Best-effort rule-based extraction of known fields from raw OCR text for a given
    /// document type. Returns a JSON object string (never null) — empty object "{}" when
    /// the document type has no known field schema or nothing matched.
    /// </summary>
    string ExtractFieldsJson(string documentType, string rawText);
}
