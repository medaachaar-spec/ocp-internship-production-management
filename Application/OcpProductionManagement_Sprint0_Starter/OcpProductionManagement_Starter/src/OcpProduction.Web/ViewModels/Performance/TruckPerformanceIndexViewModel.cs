using System.Text.Json;
using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public sealed class TruckPerformanceIndexViewModel
{
    private static readonly JsonSerializerOptions ChartJsonOptions =
        new(JsonSerializerDefaults.Web);

    public TruckPerformanceSearchCriteria Criteria { get; init; } = new();
    public TruckPerformanceReportDto Report { get; init; } = new();
    public PerformanceFilterOptions FilterOptions { get; init; } = new();
    public bool HasLoadError { get; init; }

    public PagedResult<TruckPerformanceListItemDto> Results => Report.TableRows;

    public bool HasAnyChartData =>
        Report.Charts.TripsByTruck.Count > 0 ||
        Report.Charts.TonnageByTruck.Count > 0 ||
        Report.Charts.StopsByTruck.Count > 0 ||
        Report.Charts.P2O5ByTruck.Count > 0;

    public string ChartDataJson =>
        JsonSerializer.Serialize(Report.Charts, ChartJsonOptions);

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
