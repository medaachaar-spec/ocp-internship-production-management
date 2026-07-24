namespace OcpProduction.Application.Stops.Commands;

public sealed class CreateStopCommand
{
    public long? TripId { get; init; }

    public int TruckId { get; init; }

    public int? DriverId { get; init; }

    public int? WorkShiftId { get; init; }

    public int StopReasonId { get; init; }

    public DateTime StartTime { get; init; }

    public string? Comments { get; init; }
}
