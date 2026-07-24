using OcpProduction.Application.Trips;

namespace OcpProduction.Web.ViewModels.Trips;

public static class TripPresentation
{
    public static string GetStatusLabel(string status)
    {
        return status switch
        {
            TripStatuses.Completed => "Validé",
            TripStatuses.InProgress => "En cours",
            TripStatuses.PendingValidation => "À valider",
            "Incomplete" => "Incomplet",
            "Planned" => "Planifié",
            "Cancelled" => "Annulé",
            _ => status
        };
    }

    public static string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            TripStatuses.Completed => "text-bg-success",
            TripStatuses.InProgress => "text-bg-primary",
            TripStatuses.PendingValidation => "text-bg-warning",
            "Incomplete" => "text-bg-warning",
            "Planned" => "text-bg-secondary",
            "Cancelled" => "text-bg-dark",
            _ => "text-bg-secondary"
        };
    }

    public static string FormatDuration(int? durationMinutes)
    {
        if (durationMinutes is not { } totalMinutes)
        {
            return "—";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours == 0)
        {
            return $"{minutes} min";
        }

        return minutes == 0
            ? $"{hours} h"
            : $"{hours} h {minutes} min";
    }
}
