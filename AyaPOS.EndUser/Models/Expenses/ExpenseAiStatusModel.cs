namespace Ayapos.EndUser.Models.Expenses;

public sealed class ExpenseAiStatusModel
{
    public bool Enabled { get; set; }
    public string Message { get; set; } = string.Empty;
}
