using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Web.ViewModels.Stops;

public static class StopPresentation
{
    public static string GetStateLabel(string? state) => state switch
    {
        StopStates.Open => "Ouvert",
        StopStates.Closed => "Clôturé",
        _ => "État inconnu"
    };

    public static string GetStateBadgeClass(string? state) => state switch
    {
        StopStates.Open => "text-bg-danger",
        StopStates.Closed => "text-bg-success",
        _ => "text-bg-secondary"
    };

    public static string GetValidationStatusLabel(string? status) => status switch
    {
        StopValidationStatuses.Pending => "En attente",
        StopValidationStatuses.Validated => "Validé",
        StopValidationStatuses.Rejected => "Rejeté",
        null or "" => "Non renseigné",
        _ => status
    };

    public static string GetValidationStatusBadgeClass(string? status) => status switch
    {
        StopValidationStatuses.Pending => "text-bg-warning",
        StopValidationStatuses.Validated => "text-bg-success",
        StopValidationStatuses.Rejected => "text-bg-danger",
        _ => "text-bg-secondary"
    };

    public static string FormatDuration(int? durationMinutes)
    {
        if (durationMinutes is null)
        {
            return "En cours";
        }

        var hours = durationMinutes.Value / 60;
        var minutes = durationMinutes.Value % 60;

        if (hours == 0)
        {
            return $"{minutes} min";
        }

        return minutes == 0
            ? $"{hours} h"
            : $"{hours} h {minutes} min";
    }

    public static string FormatObservation(string? observation) =>
        string.IsNullOrWhiteSpace(observation) ? "—" : observation;
}
