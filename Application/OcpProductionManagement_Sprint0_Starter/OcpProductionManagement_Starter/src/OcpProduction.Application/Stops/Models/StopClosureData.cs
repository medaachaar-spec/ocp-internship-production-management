namespace OcpProduction.Application.Stops.Models;

public sealed class StopClosureData
{
    public long StopId { get; init; }

    public long? TripId { get; init; }

    public required string TruckCode { get; init; }

    public required string StopReasonCode { get; init; }

    public required string StopReasonName { get; init; }

    public string? DriverCode { get; init; }

    public string? DriverName { get; init; }

    public string? WorkShiftName { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public required string ValidationStatus { get; init; }

    public string? Observation { get; init; }
}
