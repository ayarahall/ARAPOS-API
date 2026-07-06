using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Ayapos.Api.Services.Documents;

/// <summary>
/// OCR via the Tesseract CLI (installed in the Docker image, see Dockerfile) rather than a
/// native .NET binding — shelling out is more robust in a container and avoids native
/// interop/build issues. PDFs are rasterized to PNG pages first via Poppler's pdftoppm,
/// also installed in the image.
/// </summary>
public sealed class TesseractCliOcrService : IOcrService
{
    private readonly ILogger<TesseractCliOcrService> _logger;

    public TesseractCliOcrService(ILogger<TesseractCliOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(byte[] fileBytes, string mimeType, string languageHint, CancellationToken ct)
    {
        var lang = languageHint switch
        {
            "ar" => "ara",
            "en" => "eng",
            _ => "ara+eng",
        };

        var workDir = Path.Combine(Path.GetTempPath(), "doc-ocr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            var imagePaths = new List<string>();

            if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pdfPath = Path.Combine(workDir, "input.pdf");
                await File.WriteAllBytesAsync(pdfPath, fileBytes, ct);

                var prefix = Path.Combine(workDir, "page");
                await RunProcessAsync("pdftoppm", $"-png -r 200 \"{pdfPath}\" \"{prefix}\"", ct);

                imagePaths = Directory.GetFiles(workDir, "page*.png")
                    .Select(p => new { Path = p, Page = ExtractPageNumber(p) })
                    .OrderBy(x => x.Page)
                    .Select(x => x.Path)
                    .Take(20) // safety cap
                    .ToList();

                if (imagePaths.Count == 0)
                    throw new InvalidOperationException("pdftoppm produced no page images — the PDF may be corrupted or unreadable.");
            }
            else
            {
                var ext = string.Equals(mimeType, "image/png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
                var imagePath = Path.Combine(workDir, $"input.{ext}");
                await File.WriteAllBytesAsync(imagePath, fileBytes, ct);
                imagePaths.Add(imagePath);
            }

            var sb = new StringBuilder();
            foreach (var imagePath in imagePaths)
            {
                var outputBase = Path.Combine(workDir, "out-" + Path.GetFileNameWithoutExtension(imagePath));
                await RunProcessAsync("tesseract", $"\"{imagePath}\" \"{outputBase}\" -l {lang}", ct);

                var txtPath = outputBase + ".txt";
                if (File.Exists(txtPath))
                {
                    sb.AppendLine(await File.ReadAllTextAsync(txtPath, ct));
                }
            }

            return sb.ToString().Trim();
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up OCR temp dir {Dir}", workDir); }
        }
    }

    private static int ExtractPageNumber(string path)
    {
        var match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"(\d+)$");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} exited with code {process.ExitCode}: {stderr.Trim()}");
        }
    }
}
