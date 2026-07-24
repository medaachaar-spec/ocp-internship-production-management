using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;

namespace OcpProduction.Web.ViewModels.Quality;

public sealed class QualityDetailsViewModel
{
    public required QualityResultDetailsDto Result { get; init; }

    public required QualitySearchCriteria ReturnCriteria { get; init; }

    public bool CanViewTrip { get; init; }

    public bool CanEdit => QualityEditability.IsEditable(Result.Status);

    public Dictionary<string, string> ReturnRouteValues =>
        QualityNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> EditRouteValues =>
        QualityNavigation.ForResult(ReturnCriteria, Result.QualityResultId);
}
