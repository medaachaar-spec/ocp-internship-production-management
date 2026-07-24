using System.Globalization;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class PendingValidationIndexViewModel
{
    public required TripValidationSearchCriteria Criteria { get; init; }

    public required PagedResult<PendingTripListItemDto> Results { get; init; }

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
            [nameof(TripValidationSearchCriteria.Page)] = criteria.Page.ToString(CultureInfo.InvariantCulture),
            [nameof(TripValidationSearchCriteria.PageSize)] = criteria.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        AddIfPresent(values, nameof(TripValidationSearchCriteria.Search), criteria.Search);
        AddIfPresent(values, nameof(TripValidationSearchCriteria.DateFrom), criteria.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(values, nameof(TripValidationSearchCriteria.DateTo), criteria.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return values;
    }

    public Dictionary<string, string> ReviewRouteValues(long tripId) => new()
    {
        ["id"] = tripId.ToString(CultureInfo.InvariantCulture),
        ["fromValidation"] = bool.TrueString.ToLowerInvariant()
    };

    private static void AddIfPresent(
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
