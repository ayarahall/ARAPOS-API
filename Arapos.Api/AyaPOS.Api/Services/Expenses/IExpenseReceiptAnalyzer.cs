using Ayapos.Api.Contracts.Expenses;

namespace Ayapos.Api.Services.Expenses;

public interface IExpenseReceiptAnalyzer
{
    bool IsConfigured { get; }
    Task<AnalyzeExpenseReceiptResponse> AnalyzeAsync(string fileName, string contentType, byte[] bytes, CancellationToken ct = default);
}
