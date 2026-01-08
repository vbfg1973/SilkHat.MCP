using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Extensions;

public readonly record struct PagingParameters(int PageNumber, int PageSize)
{
    public int Skip => (PageNumber - 1) * PageSize;
}

public static class PagingExtensions
{
    public static PagingParameters ResolvePaging(this PagingQuery? query)
    {
        var pageNumber = query?.PageNumber ?? PagingDefaults.PageNumber;
        var pageSize = query?.PageSize ?? PagingDefaults.PageSize;

        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, PagingDefaults.MaxPageSize);

        return new PagingParameters(pageNumber, pageSize);
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagingParameters paging,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, paging.PageNumber, paging.PageSize, totalCount);
    }

    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> items, PagingParameters paging)
    {
        var list = items.ToList();
        var totalCount = list.Count;
        var pageItems = list
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToList();

        return new PagedResult<T>(pageItems, paging.PageNumber, paging.PageSize, totalCount);
    }
}
