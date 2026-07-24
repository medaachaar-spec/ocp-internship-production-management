using System.Globalization;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class PendingStopValidationIndexViewModel
{
    public required StopValidationSearchCriteria Criteria { get; init; }

    public required PagedResult<PendingStopListItemDto> Results { get; init; }

    public required StopFilterOptions FilterOptions { get; init; }

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
        var criteria = Criteria.WithPage(page);
        var values = new Dictionary<string, string>
        {
            [nameof(StopValidationSearchCriteria.Page)] = criteria.Page.ToString(CultureInfo.InvariantCulture),
            [nameof(StopValidationSearchCriteria.PageSize)] = criteria.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        Add(values, nameof(StopValidationSearchCriteria.Search), criteria.Search);
        Add(values, nameof(StopValidationSearchCriteria.DateFrom), FormatDate(criteria.DateFrom));
        Add(values, nameof(StopValidationSearchCriteria.DateTo), FormatDate(criteria.DateTo));
        Add(values, nameof(StopValidationSearchCriteria.TruckId), FormatNumber(criteria.TruckId));
        Add(values, nameof(StopValidationSearchCriteria.StopReasonId), FormatNumber(criteria.StopReasonId));
        return values;
    }

    public Dictionary<string, string> ReviewRouteValues(long stopId) => new()
    {
        ["id"] = stopId.ToString(CultureInfo.InvariantCulture),
        ["fromValidation"] = bool.TrueString.ToLowerInvariant()
    };

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatNumber<T>(T? value)
        where T : struct, IFormattable =>
        value?.ToString(null, CultureInfo.InvariantCulture);

    private static void Add(
        IDictionary<string, string> values,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }
}
