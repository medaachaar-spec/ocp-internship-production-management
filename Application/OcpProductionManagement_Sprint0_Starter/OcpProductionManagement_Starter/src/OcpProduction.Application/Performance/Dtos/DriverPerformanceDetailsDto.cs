namespace OcpProduction.Application.Performance.Dtos;

public sealed class DriverPerformanceDetailsDto
{
    public int DriverId { get; init; }
    public string DriverCode { get; init; } = string.Empty;
    public string DriverFullName { get; init; } = string.Empty;
    public string DriverStatus { get; init; } = string.Empty;
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
    public long? TripsWithStops { get; init; }
    public long? TotalLinkedStopCount { get; init; }
    public long? TotalLinkedStopDurationMinutes { get; init; }
    public decimal? AverageLinkedStopDurationMinutes { get; init; }
    public int? MaximumLinkedStopDurationMinutes { get; init; }
    public bool HasLinkedStops { get; init; }
    public long? QualityResultCount { get; init; }
    public decimal? AverageP2O5 { get; init; }
    public decimal? MinimumP2O5 { get; init; }
    public decimal? MaximumP2O5 { get; init; }
    public bool HasQualityResults { get; init; }
}
