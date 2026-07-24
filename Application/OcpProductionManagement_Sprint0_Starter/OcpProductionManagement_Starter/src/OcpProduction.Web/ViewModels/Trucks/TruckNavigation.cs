using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Web.ViewModels.Trucks;

public static class TruckNavigation
{
    public static Dictionary<string, string?> ForList(TruckSearchCriteria criteria, int? page = null)
    {
        var normalized = criteria.Normalize();
        return new Dictionary<string, string?>
        {
            [nameof(criteria.Search)] = normalized.Search,
            [nameof(criteria.Status)] = normalized.Status,
            [nameof(criteria.Model)] = normalized.Model,
            [nameof(criteria.Page)] = (page ?? normalized.Page).ToString(),
            [nameof(criteria.PageSize)] = normalized.PageSize.ToString()
        };
    }

    public static Dictionary<string, string?> ForDetails(TruckSearchCriteria criteria, int id)
    {
        var values = ForList(criteria);
        values["id"] = id.ToString();
        return values;
    }
}
