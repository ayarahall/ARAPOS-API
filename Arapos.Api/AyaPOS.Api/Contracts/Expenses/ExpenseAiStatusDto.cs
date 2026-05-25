namespace Ayapos.Api.Contracts.Expenses;

public sealed class ExpenseAiStatusDto
{
    public bool Enabled { get; init; }
    public string Message { get; init; } = string.Empty;
}
