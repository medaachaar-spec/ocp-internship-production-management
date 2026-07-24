using OcpProduction.Application.Drivers.Dtos;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Drivers;

public sealed class DriverIndexViewModel
{
    public required DriverSearchCriteria Criteria { get; init; }
    public required DriverFilterOptions FilterOptions { get; init; }
    public required PagedResult<DriverListItemDto> Results { get; init; }

    public IEnumerable<int> VisiblePages
    {
        get
        {
            var first = Math.Max(1, Results.Page - 2);
            var last = Math.Min(Results.TotalPages, Results.Page + 2);
            return last < first ? [] : Enumerable.Range(first, last - first + 1);
        }
    }

    public Dictionary<string, string?> RouteValuesFor(int page) => DriverNavigation.ForList(Criteria, page);
    public Dictionary<string, string?> DetailsRouteValues(int id) => DriverNavigation.ForDetails(Criteria, id);
}
