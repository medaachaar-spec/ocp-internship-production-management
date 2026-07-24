using System.Globalization;
using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public static class StopNavigation
{
    public static Dictionary<string, string> ForList(
        StopSearchCriteria criteria,
        int? page = null)
    {
        var normalized = criteria.Normalize();
        var values = new Dictionary<string, string>();

        Add(values, nameof(StopSearchCriteria.Search), normalized.Search);
        Add(values, nameof(StopSearchCriteria.DateFrom), FormatDate(normalized.DateFrom));
        Add(values, nameof(StopSearchCriteria.DateTo), FormatDate(normalized.DateTo));
        Add(values, nameof(StopSearchCriteria.TruckId), FormatNumber(normalized.TruckId));
        Add(values, nameof(StopSearchCriteria.StopReasonId), FormatNumber(normalized.StopReasonId));
        Add(values, nameof(StopSearchCriteria.State), normalized.State);
        Add(values, nameof(StopSearchCriteria.ValidationStatus), normalized.ValidationStatus);
        Add(values, nameof(StopSearchCriteria.TripId), FormatNumber(normalized.TripId));
        Add(values, nameof(StopSearchCriteria.Page), (page ?? normalized.Page).ToString(CultureInfo.InvariantCulture));
        Add(values, nameof(StopSearchCriteria.PageSize), normalized.PageSize.ToString(CultureInfo.InvariantCulture));

        return values;
    }

    public static Dictionary<string, string> ForStop(
        StopSearchCriteria criteria,
        long stopId,
        int? page = null)
    {
        var values = ForList(criteria, page);
        values["id"] = stopId.ToString(CultureInfo.InvariantCulture);
        return values;
    }

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
