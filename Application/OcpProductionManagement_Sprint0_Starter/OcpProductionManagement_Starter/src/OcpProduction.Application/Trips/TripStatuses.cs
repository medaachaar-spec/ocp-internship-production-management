namespace OcpProduction.Application.Trips;

public static class TripStatuses
{
    public const string InProgress = "InProgress";
    public const string PendingValidation = "PendingValidation";
    public const string Completed = "Completed";

    public static bool IsEditable(string? status) =>
        string.Equals(status, InProgress, StringComparison.Ordinal);

    public static bool CanBeClosed(string? status) => IsEditable(status);

    public static bool CanBeValidated(string? status) =>
        string.Equals(status, PendingValidation, StringComparison.Ordinal);

    public static bool CanTransition(string? currentStatus, string? nextStatus) =>
        (string.Equals(currentStatus, InProgress, StringComparison.Ordinal) &&
         string.Equals(nextStatus, PendingValidation, StringComparison.Ordinal)) ||
        (string.Equals(currentStatus, PendingValidation, StringComparison.Ordinal) &&
         string.Equals(nextStatus, Completed, StringComparison.Ordinal));
}
