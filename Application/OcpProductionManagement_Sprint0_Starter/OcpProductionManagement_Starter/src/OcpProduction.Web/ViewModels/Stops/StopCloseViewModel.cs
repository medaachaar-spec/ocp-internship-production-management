using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class StopCloseViewModel
{
    public required StopCloseInputModel Input { get; init; }

    public required StopClosureData Stop { get; init; }

    public required StopSearchCriteria ReturnCriteria { get; init; }

    public Dictionary<string, string> DetailsRouteValues =>
        StopNavigation.ForStop(ReturnCriteria, Stop.StopId);
}
