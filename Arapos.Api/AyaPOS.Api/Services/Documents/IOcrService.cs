namespace Ayapos.Api.Services.Documents;

public interface IOcrService
{
    /// <summary>
    /// Extracts raw text from a PDF/JPG/PNG file using Tesseract (+ Poppler for PDFs).
    /// Throws on failure — caller is responsible for marking the document FAILED.
    /// </summary>
    Task<string> ExtractTextAsync(byte[] fileBytes, string mimeType, string languageHint, CancellationToken ct);
}
