using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Quality;

public sealed class QualityIndexViewModel
{
    public required QualitySearchCriteria Criteria { get; init; }

    public required PagedResult<QualityResultListItemDto> Results { get; init; }

    public required QualityFilterOptions FilterOptions { get; init; }

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
        QualityNavigation.ForList(Criteria, page);

    public Dictionary<string, string> DetailsRouteValues(long qualityResultId) =>
        QualityNavigation.ForResult(Criteria, qualityResultId, Results.Page);

    public Dictionary<string, string> EditRouteValues(long qualityResultId) =>
        QualityNavigation.ForResult(Criteria, qualityResultId, Results.Page);

    public Dictionary<string, string> CreateRouteValues =>
        QualityNavigation.ForList(Criteria, Results.Page);
}
