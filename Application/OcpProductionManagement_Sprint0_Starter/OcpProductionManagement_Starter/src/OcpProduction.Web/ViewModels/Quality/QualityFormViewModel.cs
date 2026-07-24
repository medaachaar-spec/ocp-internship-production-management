using OcpProduction.Application.Quality.Models;

namespace OcpProduction.Web.ViewModels.Quality;

public interface IQualityFormViewModel
{
    QualityInputModelBase Input { get; }

    QualityFormOptions Options { get; }

    QualitySearchCriteria ReturnCriteria { get; }

    string PageTitle { get; }

    string SubmitLabel { get; }
}

public sealed class QualityFormViewModel<TInput> : IQualityFormViewModel
    where TInput : QualityInputModelBase
{
    public required TInput Input { get; init; }

    public required QualityFormOptions Options { get; init; }

    public required QualitySearchCriteria ReturnCriteria { get; init; }

    public required string PageTitle { get; init; }

    public required string SubmitLabel { get; init; }

    public string? CurrentStatus { get; init; }

    QualityInputModelBase IQualityFormViewModel.Input => Input;

    public Dictionary<string, string> ReturnRouteValues =>
        QualityNavigation.ForList(ReturnCriteria);

    public Dictionary<string, string> DetailsRouteValues(long qualityResultId) =>
        QualityNavigation.ForResult(ReturnCriteria, qualityResultId);
}
