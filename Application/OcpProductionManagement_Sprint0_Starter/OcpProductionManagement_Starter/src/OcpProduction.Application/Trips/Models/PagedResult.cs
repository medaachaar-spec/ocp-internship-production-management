namespace OcpProduction.Application.Trips.Models;

public sealed class PagedResult<T>
{
    private PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalItems,
        int totalPages)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = totalPages;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public long TotalItems { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => TotalPages > 0 && Page < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (totalItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalItems));
        }

        var totalPages = totalItems == 0
            ? 0
            : checked((int)Math.Ceiling(totalItems / (double)pageSize));
        var normalizedPage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        return new PagedResult<T>(items, normalizedPage, pageSize, totalItems, totalPages);
    }
}
