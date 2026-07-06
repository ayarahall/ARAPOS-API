using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ayapos.Api.Services.Documents;

/// <summary>
/// First-pass, zero-cost field extraction: regex over known Arabic/English label variants.
/// Deliberately simple — this is a starting point meant to be refined once real document
/// layouts are seen, not a general-purpose document-understanding model. Anything it gets
/// wrong or misses is still fully visible to the user for correction during review.
/// </summary>
public sealed class RuleBasedFieldExtractor : IStructuredFieldExtractor
{
    private static readonly Dictionary<string, string[]> ServiceReceiptFields = new()
    {
        ["customerName"] = ["اسم العميل", "اسم الزبون", "العميل", "الزبون", "customer name", "customer"],
        ["service"] = ["الخدمة", "نوع الخدمة", "service", "service type"],
        ["price"] = ["السعر", "المبلغ", "الإجمالي", "الاجمالي", "price", "total", "amount"],
        ["customerPhone"] = ["رقم العميل", "رقم الهاتف", "رقم الجوال", "الهاتف", "الجوال", "phone", "mobile", "customer phone", "customer number"],
        ["changeAmount"] = ["الصرف", "الباقي", "الفكة", "change", "change amount"],
    };

    public string ExtractFieldsJson(string documentType, string rawText)
    {
        if (!string.Equals(documentType, "SERVICE_RECEIPT", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(rawText))
        {
            return "{}";
        }

        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new Dictionary<string, string?>();

        foreach (var (field, labels) in ServiceReceiptFields)
        {
            result[field] = FindFirstMatch(lines, labels);
        }

        return JsonSerializer.Serialize(result);
    }

    private static string? FindFirstMatch(string[] lines, string[] labels)
    {
        foreach (var line in lines)
        {
            foreach (var label in labels)
            {
                // "<label> [:：-]? <value>" — value is whatever remains on the line.
                var pattern = $@"^\s*{Regex.Escape(label)}\s*[:：\-]?\s*(.+)$";
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    var value = match.Groups[1].Value.Trim();
                    if (value.Length > 0)
                        return value;
                }
            }
        }
        return null;
    }
}
