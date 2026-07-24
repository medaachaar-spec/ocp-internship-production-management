namespace OcpProduction.Application.Drivers.Dtos;

public sealed class DriverDetailsDto
{
    public int DriverId { get; init; }
    public string DriverCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public long TripCount { get; init; }
    public long CompletedTripCount { get; init; }
    public decimal? TotalTonnage { get; init; }
    public decimal? AverageTripDurationMinutes { get; init; }
    public DateOnly? FirstTripDate { get; init; }
    public DateOnly? LastTripDate { get; init; }
    public long StopCount { get; init; }
    public long? TotalStopDurationMinutes { get; init; }
    public decimal? AverageStopDurationMinutes { get; init; }
}
