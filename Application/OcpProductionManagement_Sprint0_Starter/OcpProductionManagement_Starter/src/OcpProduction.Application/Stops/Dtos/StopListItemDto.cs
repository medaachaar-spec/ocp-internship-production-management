namespace OcpProduction.Application.Stops.Dtos;

public sealed class StopListItemDto
{
    public long StopId { get; init; }

    public long? TripId { get; init; }

    public int TruckId { get; init; }

    public required string TruckCode { get; init; }

    public int StopReasonId { get; init; }

    public required string StopReasonCode { get; init; }

    public required string StopReasonName { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int? DurationMinutes { get; init; }

    public required string State { get; init; }

    public required string ValidationStatus { get; init; }
}
