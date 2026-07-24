using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public sealed class TruckPerformanceIndexViewModel
{
    public TruckPerformanceSearchCriteria Criteria { get; init; } = new();
    public PagedResult<TruckPerformanceListItemDto> Results { get; init; } =
        PagedResult<TruckPerformanceListItemDto>.Create([], 1, 20, 0);
    public PerformanceFilterOptions FilterOptions { get; init; } = new();

    public IEnumerable<int> VisiblePages
    {
        get
        {
            if (Results.TotalPages == 0)
            {
                yield break;
            }

            var start = Math.Max(1, Results.Page - 2);
            var end = Math.Min(Results.TotalPages, Results.Page + 2);
            for (var page = start; page <= end; page++)
            {
                yield return page;
            }
        }
    }

    public Dictionary<string, string?> RouteValuesFor(int page)
    {
        return new Dictionary<string, string?>
        {
            ["Search"] = Criteria.Search,
            ["Status"] = Criteria.Status,
            ["Sort"] = Criteria.Sort,
            ["Page"] = page.ToString(),
            ["PageSize"] = Criteria.PageSize.ToString()
        };
    }
}
