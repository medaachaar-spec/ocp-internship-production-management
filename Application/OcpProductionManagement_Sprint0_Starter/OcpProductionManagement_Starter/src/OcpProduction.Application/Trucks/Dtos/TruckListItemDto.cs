namespace OcpProduction.Application.Trucks.Dtos;

public sealed class TruckListItemDto
{
    public int TruckId { get; init; }
    public string TruckCode { get; init; } = string.Empty;
    public string? RegistrationNumber { get; init; }
    public string? Model { get; init; }
    public decimal? RatedPayloadTonnes { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public long TripCount { get; init; }
    public long StopCount { get; init; }
    public DateTime? LastTripActivity { get; init; }
    public DateTime? LastStopActivity { get; init; }
}
