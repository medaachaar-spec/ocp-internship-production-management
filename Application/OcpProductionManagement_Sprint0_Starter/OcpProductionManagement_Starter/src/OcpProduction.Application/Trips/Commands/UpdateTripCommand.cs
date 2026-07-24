namespace OcpProduction.Application.Trips.Commands;

public sealed class UpdateTripCommand
{
    public long TripId { get; init; }

    public DateOnly TripDate { get; init; }

    public int WorkShiftId { get; init; }

    public int TruckId { get; init; }

    public int DriverId { get; init; }

    public int LoadingPointId { get; init; }

    public int DestinationId { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public decimal? Tonnage { get; init; }

    public string? Comments { get; init; }
}
