namespace SilkHat.Api.Models;

public sealed class PagingQuery
{
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
