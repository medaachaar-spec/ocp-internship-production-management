namespace OcpProduction.Application.Stops.Models;

public enum StopWorkflowErrorCode
{
    None,
    NotFound,
    InvalidState,
    Validation,
    ConcurrencyConflict,
    PersistenceFailure
}

public sealed class StopWorkflowResult
{
    private StopWorkflowResult(
        StopWorkflowErrorCode errorCode,
        long stopId,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? validationErrors)
    {
        ErrorCode = errorCode;
        StopId = stopId;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string>();
    }

    public bool Success => ErrorCode == StopWorkflowErrorCode.None;

    public long StopId { get; }

    public StopWorkflowErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string> ValidationErrors { get; }

    public static StopWorkflowResult Succeeded(long stopId) =>
        new(StopWorkflowErrorCode.None, stopId, null, null);

    public static StopWorkflowResult NotFound(long stopId) =>
        new(StopWorkflowErrorCode.NotFound, stopId, null, null);

    public static StopWorkflowResult InvalidState(long stopId, string errorMessage) =>
        new(StopWorkflowErrorCode.InvalidState, stopId, errorMessage, null);

    public static StopWorkflowResult ValidationFailed(
        long stopId,
        IReadOnlyDictionary<string, string> validationErrors) =>
        new(StopWorkflowErrorCode.Validation, stopId, null, validationErrors);

    public static StopWorkflowResult ConcurrencyConflict(long stopId, string errorMessage) =>
        new(StopWorkflowErrorCode.ConcurrencyConflict, stopId, errorMessage, null);

    public static StopWorkflowResult PersistenceFailed(long stopId, string errorMessage) =>
        new(StopWorkflowErrorCode.PersistenceFailure, stopId, errorMessage, null);
}
