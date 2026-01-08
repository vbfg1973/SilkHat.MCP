namespace SilkHat.Core.Dtos;

public static class PagingDefaults
{
    public const int PageNumber = 1;
    public const int PageSize = 50;
    public const int MaxPageSize = 200;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
