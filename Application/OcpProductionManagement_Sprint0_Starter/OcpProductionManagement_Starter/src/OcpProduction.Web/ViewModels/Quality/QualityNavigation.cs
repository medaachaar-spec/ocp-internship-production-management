using System.Globalization;
using OcpProduction.Application.Quality.Models;

namespace OcpProduction.Web.ViewModels.Quality;

public static class QualityNavigation
{
    public static Dictionary<string, string> ForList(
        QualitySearchCriteria criteria,
        int? page = null)
    {
        var normalized = criteria.Normalize();
        var values = new Dictionary<string, string>
        {
            [nameof(QualitySearchCriteria.Page)] = (page ?? normalized.Page)
                .ToString(CultureInfo.InvariantCulture),
            [nameof(QualitySearchCriteria.PageSize)] = normalized.PageSize
                .ToString(CultureInfo.InvariantCulture)
        };

        AddIfPresent(values, nameof(QualitySearchCriteria.Search), normalized.Search);
        AddIfPresent(
            values,
            nameof(QualitySearchCriteria.DateFrom),
            normalized.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(
            values,
            nameof(QualitySearchCriteria.DateTo),
            normalized.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(values, nameof(QualitySearchCriteria.TripId), normalized.TripId);
        AddIfPresent(values, nameof(QualitySearchCriteria.TruckId), normalized.TruckId);
        AddIfPresent(values, nameof(QualitySearchCriteria.P2O5Min), normalized.P2O5Min);
        AddIfPresent(values, nameof(QualitySearchCriteria.P2O5Max), normalized.P2O5Max);
        AddIfPresent(values, nameof(QualitySearchCriteria.Status), normalized.Status);

        return values;
    }

    public static Dictionary<string, string> ForResult(
        QualitySearchCriteria criteria,
        long qualityResultId,
        int? page = null)
    {
        var values = ForList(criteria, page);
        values["id"] = qualityResultId.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    private static void AddIfPresent(
        IDictionary<string, string> values,
        string key,
        object? value)
    {
        if (value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
        {
            values[key] = Convert.ToString(value, CultureInfo.InvariantCulture)!;
        }
    }
}
