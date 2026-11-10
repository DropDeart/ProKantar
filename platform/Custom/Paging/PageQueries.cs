using BaseForge.Core.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Platform.Custom.Paging;

/// <summary>Elle yazılan sayfalı uçların ortak sayfa okuması.</summary>
internal static class PageQueries
{
    /// <summary>
    /// Sıralı sorgudan istenen sayfayı ve toplamı okur. İlk sayfa dolmadıysa toplam satır sayısıdır; sayım sorgusu atılmaz.
    /// </summary>
    public static async Task<(List<T> Rows, int Total)> ReadPageAsync<T>(IQueryable<T> ordered, PagedRequest request, CancellationToken cancellationToken)
    {
        var rows = await ordered.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        var total = request.Skip == 0 && rows.Count < request.PageSize ? rows.Count : await ordered.CountAsync(cancellationToken);
        return (rows, total);
    }

    /// <summary>Sayfa sonucunu isteğin sayfa bilgileriyle kurar.</summary>
    public static PagedResult<T> Result<T>(IReadOnlyList<T> items, int total, PagedRequest request)
        => new() { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
}
