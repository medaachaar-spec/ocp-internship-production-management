using System.Text.Json;
using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public sealed class DriverPerformanceIndexViewModel
{
    private static readonly JsonSerializerOptions ChartJsonOptions =
        new(JsonSerializerDefaults.Web);

    public DriverPerformanceSearchCriteria Criteria { get; init; } = new();
    public DriverPerformanceReportDto Report { get; init; } = new();
    public PerformanceFilterOptions FilterOptions { get; init; } = new();
    public bool HasLoadError { get; init; }

    public PagedResult<DriverPerformanceListItemDto> Results => Report.TableRows;

    public bool HasAnyChartData =>
        Report.Charts.TripsByDriver.Count > 0 ||
        Report.Charts.TonnageByDriver.Count > 0 ||
        Report.Charts.LinkedStopsByDriver.Count > 0;

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
