namespace OcpProduction.Application.Trips.Dtos;

public sealed class TripListItemDto
{
    public long TripId { get; init; }

    public DateOnly TripDate { get; init; }

    public int TruckId { get; init; }

    public string TruckCode { get; init; } = string.Empty;

    public int DriverId { get; init; }

    public string DriverCode { get; init; } = string.Empty;

    public string DriverDisplayName { get; init; } = string.Empty;

    public int WorkShiftId { get; init; }

    public string WorkShiftCode { get; init; } = string.Empty;

    public string WorkShiftName { get; init; } = string.Empty;

    public int LoadingPointId { get; init; }

    public string LoadingPointName { get; init; } = string.Empty;

    public int DestinationId { get; init; }

    public string DestinationName { get; init; } = string.Empty;

    public DateTime StartDateTime { get; init; }

    public DateTime? EndDateTime { get; init; }

    public int? DurationMinutes { get; init; }

    public decimal? Tonnage { get; init; }

    public string Status { get; init; } = string.Empty;
}
