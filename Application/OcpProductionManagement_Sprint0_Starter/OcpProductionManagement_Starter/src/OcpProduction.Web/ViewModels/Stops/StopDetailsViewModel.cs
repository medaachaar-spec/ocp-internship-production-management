using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class StopDetailsViewModel
{
    public required StopDetailsDto Stop { get; init; }

    public required StopSearchCriteria ReturnCriteria { get; init; }

    public bool CanViewRelatedTrip { get; init; }

    public bool CanValidate { get; init; }

    public bool FromValidation { get; init; }

    public bool CanEdit => StopWorkflowRules.IsEditable(Stop.EndTime, Stop.ValidationStatus);

    public bool CanClose => StopWorkflowRules.CanClose(Stop.EndTime, Stop.ValidationStatus);

    public bool CanValidateCurrentStop =>
        CanValidate && StopWorkflowRules.CanValidate(Stop.EndTime, Stop.ValidationStatus);

    public Dictionary<string, string> ReturnRouteValues =>
        StopNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> EditRouteValues =>
        StopNavigation.ForStop(ReturnCriteria, Stop.StopId);

    public Dictionary<string, string> CloseRouteValues =>
        StopNavigation.ForStop(ReturnCriteria, Stop.StopId);
}
