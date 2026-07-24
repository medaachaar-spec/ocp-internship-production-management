namespace OcpProduction.Application.Stops.Models;

public sealed class StopEditData
{
    public long StopId { get; init; }

    public long? TripId { get; init; }

    public int TruckId { get; init; }

    public int? DriverId { get; init; }

    public int? WorkShiftId { get; init; }

    public int StopReasonId { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public string ValidationStatus { get; init; } = string.Empty;

    public string? Comments { get; init; }
}
