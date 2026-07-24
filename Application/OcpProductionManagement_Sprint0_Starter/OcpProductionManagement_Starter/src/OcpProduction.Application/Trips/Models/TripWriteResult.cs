namespace OcpProduction.Application.Trips.Models;

public enum TripWriteErrorCode
{
    None,
    Validation,
    NotFound,
    InvalidState,
    ConcurrencyConflict,
    PersistenceFailure
}

public sealed class TripWriteResult
{
    private TripWriteResult(
        TripWriteErrorCode errorCode,
        long? tripId,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? validationErrors)
    {
        ErrorCode = errorCode;
        TripId = tripId;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string>();
    }

    public bool Success => ErrorCode == TripWriteErrorCode.None;

    public long? TripId { get; }

    public TripWriteErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string> ValidationErrors { get; }

    public static TripWriteResult Succeeded(long tripId) =>
        new(TripWriteErrorCode.None, tripId, null, null);

    public static TripWriteResult ValidationFailed(
        IReadOnlyDictionary<string, string> validationErrors) =>
        new(TripWriteErrorCode.Validation, null, null, validationErrors);

    public static TripWriteResult NotFound(long tripId) =>
        new(TripWriteErrorCode.NotFound, tripId, null, null);

    public static TripWriteResult InvalidState(long tripId, string errorMessage) =>
        new(TripWriteErrorCode.InvalidState, tripId, errorMessage, null);

    public static TripWriteResult ConcurrencyConflict(
        long tripId,
        string errorMessage) =>
        new(TripWriteErrorCode.ConcurrencyConflict, tripId, errorMessage, null);

    public static TripWriteResult PersistenceFailed(string errorMessage) =>
        new(TripWriteErrorCode.PersistenceFailure, null, errorMessage, null);
}
