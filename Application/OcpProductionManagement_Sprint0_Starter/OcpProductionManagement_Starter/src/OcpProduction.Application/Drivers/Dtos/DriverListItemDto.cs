namespace OcpProduction.Application.Drivers.Dtos;

public sealed class DriverListItemDto
{
    public int DriverId { get; init; }
    public string DriverCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public long TripCount { get; init; }
    public long StopCount { get; init; }
    public DateTime? LastTripActivity { get; init; }
    public DateTime? LastStopActivity { get; init; }
}
