using OcpProduction.Application.Drivers.Models;

namespace OcpProduction.Web.ViewModels.Drivers;

public static class DriverNavigation
{
    public static Dictionary<string, string?> ForList(DriverSearchCriteria criteria, int? page = null)
    {
        var normalized = criteria.Normalize();
        return new Dictionary<string, string?>
        {
            [nameof(criteria.Search)] = normalized.Search,
            [nameof(criteria.Status)] = normalized.Status,
            [nameof(criteria.Page)] = (page ?? normalized.Page).ToString(),
            [nameof(criteria.PageSize)] = normalized.PageSize.ToString()
        };
    }

    public static Dictionary<string, string?> ForDetails(DriverSearchCriteria criteria, int id)
    {
        var values = ForList(criteria);
        values["id"] = id.ToString();
        return values;
    }
}
