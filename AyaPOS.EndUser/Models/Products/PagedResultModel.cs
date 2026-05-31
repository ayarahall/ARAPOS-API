namespace Ayapos.EndUser.Models.Products;

public sealed class PagedResultModel<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
