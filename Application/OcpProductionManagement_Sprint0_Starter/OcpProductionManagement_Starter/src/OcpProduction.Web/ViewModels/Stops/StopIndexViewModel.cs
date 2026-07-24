using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class StopIndexViewModel
{
    public required StopSearchCriteria Criteria { get; init; }

    public required PagedResult<StopListItemDto> Results { get; init; }

    public required StopFilterOptions FilterOptions { get; init; }

    public IEnumerable<int> VisiblePages
    {
        get
        {
            var firstPage = Math.Max(1, Results.Page - 2);
            var lastPage = Math.Min(Results.TotalPages, Results.Page + 2);

            return lastPage < firstPage
                ? []
                : Enumerable.Range(firstPage, lastPage - firstPage + 1);
        }
    }

    public Dictionary<string, string> RouteValuesFor(int page) =>
        StopNavigation.ForList(Criteria, page);

    public Dictionary<string, string> DetailsRouteValues(long stopId) =>
        StopNavigation.ForStop(Criteria, stopId, Results.Page);

    public Dictionary<string, string> EditRouteValues(long stopId) =>
        StopNavigation.ForStop(Criteria, stopId, Results.Page);
}
