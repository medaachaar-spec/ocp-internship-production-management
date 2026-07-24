using OcpProduction.Application.Trips;
using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class TripDetailsViewModel
{
    public required TripDetailsDto Trip { get; init; }

    public required TripSearchCriteria ReturnCriteria { get; init; }

    public bool CanValidate { get; init; }

    public bool FromValidation { get; init; }

    public bool CanEdit => TripStatuses.IsEditable(Trip.Status);

    public bool CanClose => TripStatuses.CanBeClosed(Trip.Status);

    public bool CanValidateCurrentTrip =>
        CanValidate && TripStatuses.CanBeValidated(Trip.Status);

    public Dictionary<string, string> ReturnRouteValues =>
        TripNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> EditRouteValues =>
        TripNavigation.ForTrip(ReturnCriteria, Trip.TripId);

    public Dictionary<string, string> CloseRouteValues =>
        TripNavigation.ForTrip(ReturnCriteria, Trip.TripId);
}
