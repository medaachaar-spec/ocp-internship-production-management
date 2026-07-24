using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public interface IStopFormViewModel
{
    StopInputModelBase Input { get; }

    StopFormOptions Options { get; }

    StopSearchCriteria ReturnCriteria { get; }

    string PageTitle { get; }

    string SubmitLabel { get; }
}

public sealed class StopFormViewModel<TInput> : IStopFormViewModel
    where TInput : StopInputModelBase
{
    public required TInput Input { get; init; }

    public required StopFormOptions Options { get; init; }

    public required StopSearchCriteria ReturnCriteria { get; init; }

    public required string PageTitle { get; init; }

    public required string SubmitLabel { get; init; }

    public string? CurrentState { get; init; }

    public string? CurrentValidationStatus { get; init; }

    StopInputModelBase IStopFormViewModel.Input => Input;

    public Dictionary<string, string> ReturnRouteValues =>
        StopNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> DetailsRouteValues(long stopId) =>
        StopNavigation.ForStop(ReturnCriteria, stopId);
}
