using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public static class PerformancePresentation
{
    public static string OrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string FormatNumber(long? value) => value?.ToString("N0") ?? "—";

    public static string FormatDecimal(decimal? value, string suffix = "") => value.HasValue
        ? $"{value.Value:N2}{suffix}"
        : "—";

    public static string FormatPercent(decimal? value) => FormatDecimal(value, " %");

    public static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "—";

    public static string FormatMinutes(decimal? value) => value.HasValue
        ? $"{value.Value:N1} min"
        : "—";

    public static string FormatMinutes(long? value) => value.HasValue
        ? $"{value.Value:N0} min"
        : "—";

    public static string GetTruckStatusLabel(string status) => status switch
    {
        TruckStatusDefinitions.Available => "Disponible",
        TruckStatusDefinitions.InService => "En service",
        TruckStatusDefinitions.Maintenance => "Maintenance",
        TruckStatusDefinitions.OutOfService => "Hors service",
        _ => status
    };

    public static string GetDriverStatusLabel(string status) => status switch
    {
        DriverStatusDefinitions.Active => "Actif",
        DriverStatusDefinitions.Inactive => "Inactif",
        DriverStatusDefinitions.OnLeave => "En congé",
        _ => status
    };
}
