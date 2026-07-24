namespace OcpProduction.Application.Quality.Models;

public enum QualityWriteErrorCode
{
    None,
    Validation,
    NotFound,
    InvalidState,
    ConcurrencyConflict,
    PersistenceFailure
}

public sealed class QualityWriteResult
{
    private QualityWriteResult(
        QualityWriteErrorCode errorCode,
        long? qualityResultId,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? validationErrors)
    {
        ErrorCode = errorCode;
        QualityResultId = qualityResultId;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string>();
    }

    public bool Success => ErrorCode == QualityWriteErrorCode.None;

    public long? QualityResultId { get; }

    public QualityWriteErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string> ValidationErrors { get; }

    public static QualityWriteResult Succeeded(long qualityResultId) =>
        new(QualityWriteErrorCode.None, qualityResultId, null, null);

    public static QualityWriteResult ValidationFailed(
        IReadOnlyDictionary<string, string> validationErrors) =>
        new(QualityWriteErrorCode.Validation, null, null, validationErrors);

    public static QualityWriteResult NotFound(long qualityResultId) =>
        new(QualityWriteErrorCode.NotFound, qualityResultId, null, null);

    public static QualityWriteResult InvalidState(long qualityResultId, string errorMessage) =>
        new(QualityWriteErrorCode.InvalidState, qualityResultId, errorMessage, null);

    public static QualityWriteResult ConcurrencyConflict(long qualityResultId, string errorMessage) =>
        new(QualityWriteErrorCode.ConcurrencyConflict, qualityResultId, errorMessage, null);

    public static QualityWriteResult PersistenceFailed(string errorMessage) =>
        new(QualityWriteErrorCode.PersistenceFailure, null, errorMessage, null);
}
