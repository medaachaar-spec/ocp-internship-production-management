namespace OcpProduction.Application.ReferenceData.Models;

public sealed class ReferenceDataWriteResult
{
    private ReferenceDataWriteResult(bool succeeded, int? entityId, string message)
    {
        Succeeded = succeeded;
        EntityId = entityId;
        Message = message;
    }

    public bool Succeeded { get; }
    public int? EntityId { get; }
    public string Message { get; }

    public static ReferenceDataWriteResult Success(int entityId, string message) => new(true, entityId, message);

    public static ReferenceDataWriteResult Failure(string message) => new(false, null, message);
}
