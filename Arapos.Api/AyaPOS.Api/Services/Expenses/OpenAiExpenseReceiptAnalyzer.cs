using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ayapos.Api.Contracts.Expenses;
using Ayapos.Api.Options;
using Microsoft.Extensions.Options;

namespace Ayapos.Api.Services.Expenses;

public sealed class OpenAiExpenseReceiptAnalyzer : IExpenseReceiptAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string OpenAiApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    private readonly HttpClient _httpClient;
    private readonly OpenAiVisionOptions _options;
    private readonly string _apiKey;

    public OpenAiExpenseReceiptAnalyzer(HttpClient httpClient, IOptions<OpenAiVisionOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _apiKey = ResolveApiKey(_options);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AnalyzeExpenseReceiptResponse> AnalyzeAsync(string fileName, string contentType, byte[] bytes, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException($"AI receipt analysis is not configured yet. Add OpenAI:ApiKey or set the {OpenAiApiKeyEnvironmentVariable} environment variable first.");

        if (bytes.Length == 0)
            throw new InvalidOperationException("Receipt file is empty.");

        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4.1-mini" : _options.Model,
            response_format = new
            {
                type = "json_object"
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You extract branch expense receipt data. Return valid JSON only with keys: title, category, amount, currencyCode, expenseDate, notes, vendorName, confidence, rawSummary. amount must be numeric. confidence is between 0 and 1. expenseDate must be yyyy-MM-dd. If uncertain, still return the best guess."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Analyze this expense receipt image for a branch expense record. Today's date is {today}. Suggested categories: Supplies, Utilities, Maintenance, Transportation, Marketing, Rent, Petty Cash, Meals, Other."
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = dataUrl
                            }
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"AI receipt analysis failed: {responseBody}");

        using var document = JsonDocument.Parse(responseBody);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("AI receipt analysis returned an empty response.");

        using var resultDocument = JsonDocument.Parse(content);
        var root = resultDocument.RootElement;

        var amount = TryReadDecimal(root, "amount");
        var currencyCode = TryReadString(root, "currencyCode");
        var expenseDate = TryReadDate(root, "expenseDate") ?? DateTime.UtcNow.Date;
        var confidence = TryReadDecimal(root, "confidence");

        return new AnalyzeExpenseReceiptResponse
        {
            Title = TryReadString(root, "title") ?? Path.GetFileNameWithoutExtension(fileName),
            Category = TryReadString(root, "category") ?? "Other",
            Amount = amount > 0 ? amount : 0m,
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "AED" : currencyCode.ToUpperInvariant(),
            ExpenseDate = expenseDate,
            Notes = TryReadString(root, "notes") ?? string.Empty,
            VendorName = TryReadString(root, "vendorName") ?? string.Empty,
            Confidence = confidence,
            RawSummary = TryReadString(root, "rawSummary") ?? string.Empty
        };
    }

    private string BuildChatCompletionsUrl()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.openai.com/v1/" : _options.BaseUrl;
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            baseUrl += "/";

        return $"{baseUrl}chat/completions";
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal TryReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return 0m;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
            return numeric;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return 0m;
    }

    private static DateTime? TryReadDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        return DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    private static string ResolveApiKey(OpenAiVisionOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            return options.ApiKey.Trim();

        return Environment.GetEnvironmentVariable(OpenAiApiKeyEnvironmentVariable)?.Trim() ?? string.Empty;
    }
}
