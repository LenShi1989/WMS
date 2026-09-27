using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Wms.Application.Common;

public static class QueryableExtensions
{
    /// <summary>套用分頁並投影成 DTO，同時取回總筆數。</summary>
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        PagedQuery paging,
        Expression<Func<TEntity, TDto>> selector,
        CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(selector)
            .ToListAsync(ct);

        return new PagedResult<TDto>(items, paging.Page, paging.PageSize, total);
    }

    /// <summary>條件成立時才套用 Where，讓查詢組裝保持可讀。</summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
