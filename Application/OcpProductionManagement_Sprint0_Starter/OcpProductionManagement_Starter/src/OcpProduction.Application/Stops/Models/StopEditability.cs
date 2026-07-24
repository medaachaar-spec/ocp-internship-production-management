namespace OcpProduction.Application.Stops.Models;

public static class StopEditability
{
    public static bool IsEditable(DateTime? endTime, string? validationStatus) =>
        StopWorkflowRules.IsEditable(endTime, validationStatus);
}
