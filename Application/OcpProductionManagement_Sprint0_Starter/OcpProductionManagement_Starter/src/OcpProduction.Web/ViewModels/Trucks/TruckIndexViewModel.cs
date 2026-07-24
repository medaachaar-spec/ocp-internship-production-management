using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trucks.Dtos;
using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Web.ViewModels.Trucks;

public sealed class TruckIndexViewModel
{
    public required TruckSearchCriteria Criteria { get; init; }
    public required TruckFilterOptions FilterOptions { get; init; }
    public required PagedResult<TruckListItemDto> Results { get; init; }

    public IEnumerable<int> VisiblePages
    {
        get
        {
            var first = Math.Max(1, Results.Page - 2);
            var last = Math.Min(Results.TotalPages, Results.Page + 2);
            return last < first ? [] : Enumerable.Range(first, last - first + 1);
        }
    }

    public Dictionary<string, string?> RouteValuesFor(int page) => TruckNavigation.ForList(Criteria, page);
    public Dictionary<string, string?> DetailsRouteValues(int id) => TruckNavigation.ForDetails(Criteria, id);
}
