namespace OcpProduction.Application.Performance.Dtos;

public sealed class DriverPerformanceListItemDto
{
    public int DriverId { get; init; }
    public string DriverCode { get; init; } = string.Empty;
    public string DriverFullName { get; init; } = string.Empty;
    public string DriverStatus { get; init; } = string.Empty;
    public long TotalTrips { get; init; }
    public long CompletedTrips { get; init; }
    public decimal? CompletionRatePercent { get; init; }
    public decimal? TotalTonnage { get; init; }
    public decimal CompletedTonnage { get; init; }
    public decimal? AverageTripDurationMinutes { get; init; }
    public long? TotalLinkedStopCount { get; init; }
    public long? TotalLinkedStopDurationMinutes { get; init; }
    public decimal? AverageP2O5 { get; init; }
    public DateOnly? LastTripDate { get; init; }
}
