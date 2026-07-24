using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public interface ITripFormViewModel
{
    TripInputModelBase Input { get; }

    TripFormOptions Options { get; }

    TripSearchCriteria ReturnCriteria { get; }
}

public sealed class TripFormViewModel<TInput> : ITripFormViewModel
    where TInput : TripInputModelBase
{
    public required TInput Input { get; init; }

    public required TripFormOptions Options { get; init; }

    public required TripSearchCriteria ReturnCriteria { get; init; }

    public string? CurrentStatus { get; init; }

    TripInputModelBase ITripFormViewModel.Input => Input;

    public Dictionary<string, string> ReturnRouteValues =>
        TripNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> DetailsRouteValues(long tripId) =>
        TripNavigation.ForTrip(ReturnCriteria, tripId);
}
