namespace OcpProduction.Application.Trucks.Dtos;

public sealed class TruckDetailsDto
{
    public int TruckId { get; init; }
    public string TruckCode { get; init; } = string.Empty;
    public string? RegistrationNumber { get; init; }
    public string? Model { get; init; }
    public decimal? RatedPayloadTonnes { get; init; }
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
    public long? TotalDowntimeMinutes { get; init; }
    public decimal? AverageStopDurationMinutes { get; init; }
}
