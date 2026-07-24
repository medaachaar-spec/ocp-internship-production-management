using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class TripCloseViewModel
{
    public required TripCloseInputModel Input { get; init; }

    public required TripClosureData Trip { get; init; }

    public required TripSearchCriteria ReturnCriteria { get; init; }

    public Dictionary<string, string> DetailsRouteValues =>
        TripNavigation.ForTrip(ReturnCriteria, Trip.TripId);
}
