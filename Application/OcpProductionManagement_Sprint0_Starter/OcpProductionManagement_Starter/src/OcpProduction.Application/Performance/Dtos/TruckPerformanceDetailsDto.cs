namespace OcpProduction.Application.Performance.Dtos;

public sealed class TruckPerformanceDetailsDto
{
    public int TruckId { get; init; }
    public string TruckCode { get; init; } = string.Empty;
    public string? TruckModel { get; init; }
    public string TruckStatus { get; init; } = string.Empty;
    public decimal? RatedPayloadTonnes { get; init; }
    public long TotalTrips { get; init; }
    public long CompletedTrips { get; init; }
    public long IncompleteTrips { get; init; }
    public decimal? CompletionRatePercent { get; init; }
    public long? ActiveTripDays { get; init; }
    public DateOnly? FirstTripDate { get; init; }
    public DateOnly? LastTripDate { get; init; }
    public decimal? TripsPerActiveDay { get; init; }
    public bool HasTrips { get; init; }
    public decimal? TotalTonnage { get; init; }
    public decimal? AveragePayloadTonnes { get; init; }
    public decimal? MinimumPayloadTonnes { get; init; }
    public decimal? MaximumPayloadTonnes { get; init; }
    public decimal? AveragePayloadUtilizationPercent { get; init; }
    public decimal? AverageTripDurationMinutes { get; init; }
    public int? MinimumTripDurationMinutes { get; init; }
    public int? MaximumTripDurationMinutes { get; init; }
    public long? TotalStopCount { get; init; }
    public long? CompletedStopCount { get; init; }
    public long? TotalDowntimeMinutes { get; init; }
    public decimal? AverageStopDurationMinutes { get; init; }
    public int? MaximumStopDurationMinutes { get; init; }
    public long? StopsWithTripCount { get; init; }
    public long? StopsWithoutTripCount { get; init; }
    public bool HasStops { get; init; }
    public long? QualityResultCount { get; init; }
    public decimal? AverageP2O5 { get; init; }
    public decimal? MinimumP2O5 { get; init; }
    public decimal? MaximumP2O5 { get; init; }
    public bool HasQualityResults { get; init; }
}
