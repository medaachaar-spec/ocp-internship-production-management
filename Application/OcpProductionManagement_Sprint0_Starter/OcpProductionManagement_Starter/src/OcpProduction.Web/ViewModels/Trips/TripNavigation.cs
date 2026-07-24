using System.Globalization;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Web.ViewModels.Trips;

public static class TripNavigation
{
    public static Dictionary<string, string> ForList(
        TripSearchCriteria criteria,
        int? page = null)
    {
        var normalized = criteria.Normalize();
        var values = new Dictionary<string, string>
        {
            [nameof(TripSearchCriteria.Page)] = (page ?? normalized.Page)
                .ToString(CultureInfo.InvariantCulture),
            [nameof(TripSearchCriteria.PageSize)] = normalized.PageSize
                .ToString(CultureInfo.InvariantCulture)
        };

        AddIfPresent(values, nameof(TripSearchCriteria.Search), normalized.Search);
        AddIfPresent(
            values,
            nameof(TripSearchCriteria.DateFrom),
            normalized.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(
            values,
            nameof(TripSearchCriteria.DateTo),
            normalized.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(values, nameof(TripSearchCriteria.TruckId), normalized.TruckId);
        AddIfPresent(values, nameof(TripSearchCriteria.DriverId), normalized.DriverId);
        AddIfPresent(values, nameof(TripSearchCriteria.WorkShiftId), normalized.WorkShiftId);
        AddIfPresent(values, nameof(TripSearchCriteria.Status), normalized.Status);

        return values;
    }

    public static Dictionary<string, string> ForTrip(
        TripSearchCriteria criteria,
        long tripId,
        int? page = null)
    {
        var values = ForList(criteria, page);
        values["id"] = tripId.ToString(CultureInfo.InvariantCulture);
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
