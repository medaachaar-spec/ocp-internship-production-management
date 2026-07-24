namespace OcpProduction.Application.Trips.Dtos;

public sealed class TripDetailsDto
{
    public long TripId { get; init; }

    public DateOnly TripDate { get; init; }

    public int TruckId { get; init; }

    public string TruckCode { get; init; } = string.Empty;

    public string? RegistrationNumber { get; init; }

    public int DriverId { get; init; }

    public string DriverCode { get; init; } = string.Empty;

    public string DriverDisplayName { get; init; } = string.Empty;

    public int WorkShiftId { get; init; }

    public string WorkShiftCode { get; init; } = string.Empty;

    public string WorkShiftName { get; init; } = string.Empty;

    public int LoadingPointId { get; init; }

    public string LoadingPointCode { get; init; } = string.Empty;

    public string LoadingPointName { get; init; } = string.Empty;

    public string? LoadingZoneName { get; init; }

    public int DestinationId { get; init; }

    public string DestinationCode { get; init; } = string.Empty;

    public string DestinationName { get; init; } = string.Empty;

    public string? DestinationType { get; init; }

    public DateTime StartDateTime { get; init; }

    public DateTime? EndDateTime { get; init; }

    public int? DurationMinutes { get; init; }

    public bool IsLongTrip { get; init; }

    public decimal? Tonnage { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? Comments { get; init; }

    public DateTime CreatedAt { get; init; }

    public long StopCount { get; init; }

    public long TotalStopDurationMinutes { get; init; }

    public long QualityResultCount { get; init; }

    public decimal? LatestP2O5 { get; init; }
}
