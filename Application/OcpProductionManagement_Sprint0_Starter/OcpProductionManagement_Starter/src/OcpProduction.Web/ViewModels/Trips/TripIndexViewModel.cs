using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class TripIndexViewModel
{
    public required TripSearchCriteria Criteria { get; init; }

    public required PagedResult<TripListItemDto> Results { get; init; }

    public required TripFilterOptions FilterOptions { get; init; }

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

    public Dictionary<string, string> RouteValuesFor(int page)
    {
        return TripNavigation.ForList(Criteria, page);
    }

    public Dictionary<string, string> DetailsRouteValues(long tripId)
    {
        return TripNavigation.ForTrip(Criteria, tripId, Results.Page);
    }

    public Dictionary<string, string> EditRouteValues(long tripId)
    {
        return TripNavigation.ForTrip(Criteria, tripId, Results.Page);
    }

    public Dictionary<string, string> CloseRouteValues(long tripId)
    {
        return TripNavigation.ForTrip(Criteria, tripId, Results.Page);
    }

    public Dictionary<string, string> CreateRouteValues =>
        TripNavigation.ForList(Criteria, Results.Page);
}
