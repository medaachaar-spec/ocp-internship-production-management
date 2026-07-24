using System.Globalization;
using OcpProduction.Application.Quality.Models;

namespace OcpProduction.Web.ViewModels.Quality;

public static class QualityPresentation
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static string GetStatusLabel(string? status)
    {
        return status switch
        {
            QualityStatusDefinitions.Pending => "En attente",
            QualityStatusDefinitions.Review => "À examiner",
            QualityStatusDefinitions.AboveTarget => "Au-dessus de la cible enregistrée",
            QualityStatusDefinitions.BelowTarget => "En dessous de la cible enregistrée",
            QualityStatusDefinitions.InTarget => "Dans la cible enregistrée",
            null or "" => "—",
            _ => status
        };
    }

    public static string GetStatusBadgeClass(string? status)
    {
        return status switch
        {
            QualityStatusDefinitions.Pending => "text-bg-secondary",
            QualityStatusDefinitions.Review => "text-bg-warning",
            _ => "text-bg-light border text-dark"
        };
    }

    public static string FormatDate(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm", FrenchCulture) ?? "—";

    public static string FormatDecimal(decimal? value) =>
        value?.ToString("N3", FrenchCulture) ?? "—";

    public static string FormatMeasurement(decimal? value, string? unit)
    {
        if (!value.HasValue)
        {
            return "—";
        }

        var formattedValue = FormatDecimal(value);
        return unit switch
        {
            "P2O5Percent" => $"{formattedValue} % P₂O₅",
            null or "" => formattedValue,
            _ => $"{formattedValue} {unit}"
        };
    }

    public static string OrFallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;
}
