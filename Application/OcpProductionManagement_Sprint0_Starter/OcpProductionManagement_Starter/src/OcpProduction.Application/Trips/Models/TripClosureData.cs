namespace OcpProduction.Application.Trips.Models;

public sealed class TripClosureData
{
    public long TripId { get; init; }

    public DateOnly TripDate { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public decimal? Tonnage { get; init; }

    public string Status { get; init; } = string.Empty;

    public string TruckCode { get; init; } = string.Empty;

    public string DriverCode { get; init; } = string.Empty;

    public string DriverDisplayName { get; init; } = string.Empty;

    public string WorkShiftName { get; init; } = string.Empty;

    public string LoadingPointName { get; init; } = string.Empty;

    public string DestinationName { get; init; } = string.Empty;
}
