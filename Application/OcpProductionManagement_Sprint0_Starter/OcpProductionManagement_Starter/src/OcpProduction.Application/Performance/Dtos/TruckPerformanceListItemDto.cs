namespace OcpProduction.Application.Performance.Dtos;

public sealed class TruckPerformanceListItemDto
{
    public int TruckId { get; init; }
    public string TruckCode { get; init; } = string.Empty;
    public string? TruckModel { get; init; }
    public string TruckStatus { get; init; } = string.Empty;
    public long TotalTrips { get; init; }
    public long CompletedTrips { get; init; }
    public decimal? CompletionRatePercent { get; init; }
    public decimal? TotalTonnage { get; init; }
    public decimal CompletedTonnage { get; init; }
    public decimal? AveragePayloadUtilizationPercent { get; init; }
    public long? TotalStopCount { get; init; }
    public long? TotalDowntimeMinutes { get; init; }
    public decimal? AverageP2O5 { get; init; }
    public DateOnly? LastTripDate { get; init; }
}
