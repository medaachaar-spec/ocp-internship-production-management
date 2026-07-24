namespace OcpProduction.Application.Trips.Models;

public enum TripWorkflowErrorCode
{
    None,
    NotFound,
    InvalidState,
    Validation,
    ConcurrencyConflict,
    PersistenceFailure
}

public sealed class TripWorkflowResult
{
    private TripWorkflowResult(
        TripWorkflowErrorCode errorCode,
        long tripId,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? validationErrors)
    {
        ErrorCode = errorCode;
        TripId = tripId;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string>();
    }

    public bool Success => ErrorCode == TripWorkflowErrorCode.None;

    public long TripId { get; }

    public TripWorkflowErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string> ValidationErrors { get; }

    public static TripWorkflowResult Succeeded(long tripId) =>
        new(TripWorkflowErrorCode.None, tripId, null, null);

    public static TripWorkflowResult NotFound(long tripId) =>
        new(TripWorkflowErrorCode.NotFound, tripId, null, null);

    public static TripWorkflowResult InvalidState(long tripId, string errorMessage) =>
        new(TripWorkflowErrorCode.InvalidState, tripId, errorMessage, null);

    public static TripWorkflowResult ValidationFailed(
        long tripId,
        IReadOnlyDictionary<string, string> validationErrors) =>
        new(TripWorkflowErrorCode.Validation, tripId, null, validationErrors);

    public static TripWorkflowResult ConcurrencyConflict(long tripId, string errorMessage) =>
        new(TripWorkflowErrorCode.ConcurrencyConflict, tripId, errorMessage, null);

    public static TripWorkflowResult PersistenceFailed(long tripId, string errorMessage) =>
        new(TripWorkflowErrorCode.PersistenceFailure, tripId, errorMessage, null);
}
