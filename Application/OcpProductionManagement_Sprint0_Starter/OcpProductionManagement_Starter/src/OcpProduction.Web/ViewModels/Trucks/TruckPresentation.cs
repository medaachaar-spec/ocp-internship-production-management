using System.Globalization;
using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Web.ViewModels.Trucks;

public static class TruckPresentation
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static string GetStatusLabel(string? status) => status switch
    {
        TruckStatusDefinitions.Available => "Disponible",
        TruckStatusDefinitions.InService => "En service",
        TruckStatusDefinitions.Maintenance => "Maintenance",
        TruckStatusDefinitions.OutOfService => "Hors service",
        null or "" => "Non renseigné",
        _ => status
    };

    public static string GetStatusBadgeClass(string? status, bool isActive) => !isActive
        ? "text-bg-secondary"
        : status switch
        {
            TruckStatusDefinitions.Available or TruckStatusDefinitions.InService => "text-bg-success",
            TruckStatusDefinitions.Maintenance => "text-bg-warning",
            TruckStatusDefinitions.OutOfService => "text-bg-danger",
            _ => "text-bg-secondary"
        };

    public static string GetActiveLabel(bool isActive) => isActive ? "Actif" : "Inactif";

    public static string OrFallback(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string FormatDate(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm", FrenchCulture) ?? "—";

    public static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy", FrenchCulture) ?? "—";

    public static string FormatTonnage(decimal? value) => value.HasValue
        ? $"{value.Value.ToString("N2", FrenchCulture)} t"
        : "—";

    public static string FormatDuration(decimal? minutes) => minutes.HasValue
        ? FormatDuration((long)Math.Round(minutes.Value, MidpointRounding.AwayFromZero))
        : "—";

    public static string FormatDuration(long? minutes)
    {
        if (!minutes.HasValue)
        {
            return "—";
        }

        var hours = minutes.Value / 60;
        var remainingMinutes = minutes.Value % 60;
        return hours > 0 ? $"{hours} h {remainingMinutes:D2} min" : $"{remainingMinutes} min";
    }

    public static DateTime? LatestActivity(DateTime? trip, DateTime? stop) =>
        trip >= stop ? trip : stop;
}
