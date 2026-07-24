namespace OcpProduction.Application.Stops.Models;

public enum StopWriteErrorCode
{
    None,
    Validation,
    NotFound,
    InvalidState,
    ConcurrencyConflict,
    PersistenceFailure
}

public sealed class StopWriteResult
{
    private StopWriteResult(
        StopWriteErrorCode errorCode,
        long? stopId,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? validationErrors)
    {
        ErrorCode = errorCode;
        StopId = stopId;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string>();
    }

    public bool Success => ErrorCode == StopWriteErrorCode.None;

    public long? StopId { get; }

    public StopWriteErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string> ValidationErrors { get; }

    public static StopWriteResult Succeeded(long stopId) =>
        new(StopWriteErrorCode.None, stopId, null, null);

    public static StopWriteResult ValidationFailed(IReadOnlyDictionary<string, string> errors) =>
        new(StopWriteErrorCode.Validation, null, null, errors);

    public static StopWriteResult NotFound(long stopId) =>
        new(StopWriteErrorCode.NotFound, stopId, null, null);

    public static StopWriteResult InvalidState(long stopId, string message) =>
        new(StopWriteErrorCode.InvalidState, stopId, message, null);

    public static StopWriteResult ConcurrencyConflict(long stopId, string message) =>
        new(StopWriteErrorCode.ConcurrencyConflict, stopId, message, null);

    public static StopWriteResult PersistenceFailed(string message) =>
        new(StopWriteErrorCode.PersistenceFailure, null, message, null);
}
