using System.Text.RegularExpressions;

namespace OcpProduction.Application.Performance.Models;

public sealed partial class DriverPerformanceSearchCriteria
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string Sort { get; init; } = DriverPerformanceSort.TotalTripsDesc;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public DriverPerformanceSearchCriteria Normalize()
    {
        var search = NormalizeWhitespace(Search);
        var status = string.IsNullOrWhiteSpace(Status) ? null : Status.Trim();
        var sort = DriverPerformanceSort.All.Contains(Sort, StringComparer.Ordinal)
            ? Sort
            : DriverPerformanceSort.TotalTripsDesc;

        return new DriverPerformanceSearchCriteria
        {
            Search = search,
            Status = status,
            Sort = sort,
            Page = Math.Max(Page, 1),
            PageSize = PageSize is 10 or 20 or 50 ? PageSize : 20
        };
    }

    public DriverPerformanceSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        return new DriverPerformanceSearchCriteria
        {
            Search = normalized.Search,
            Status = normalized.Status,
            Sort = normalized.Sort,
            Page = Math.Max(page, 1),
            PageSize = normalized.PageSize
        };
    }

    private static string? NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = WhitespaceRegex().Replace(value.Trim(), " ");
        return normalized.Length == 0 ? null : normalized;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
