namespace OcpProduction.Application.Stops.Models;

public static class StopWorkflowRules
{
    public static bool IsOpen(DateTime? endTime) => endTime is null;

    public static bool IsClosed(DateTime? endTime) => endTime is not null;

    public static bool IsPending(string? validationStatus) =>
        string.Equals(
            validationStatus,
            StopValidationStatuses.Pending,
            StringComparison.Ordinal);

    public static bool IsValidated(string? validationStatus) =>
        string.Equals(
            validationStatus,
            StopValidationStatuses.Validated,
            StringComparison.Ordinal);

    public static bool IsEditable(DateTime? endTime, string? validationStatus) =>
        IsOpen(endTime) && IsPending(validationStatus);

    public static bool CanClose(DateTime? endTime, string? validationStatus) =>
        IsEditable(endTime, validationStatus);

    public static bool CanValidate(DateTime? endTime, string? validationStatus) =>
        IsClosed(endTime) && IsPending(validationStatus);
}
