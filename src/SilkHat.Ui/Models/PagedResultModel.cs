namespace SilkHat.Ui.Models
{
    public sealed record PagedResultModel<T>(
        IReadOnlyList<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount);
}