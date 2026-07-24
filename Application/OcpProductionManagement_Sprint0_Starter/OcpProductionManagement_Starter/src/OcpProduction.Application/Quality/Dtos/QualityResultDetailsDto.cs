namespace OcpProduction.Application.Quality.Dtos;

public sealed class QualityResultDetailsDto
{
    public long QualityResultId { get; init; }

    public long? TripId { get; init; }

    public DateTime AnalysisDate { get; init; }

    public decimal QualityValue { get; init; }

    public string QualityUnit { get; init; } = string.Empty;

    public decimal? TargetValue { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? Comments { get; init; }

    public DateTime CreatedAt { get; init; }

    public int? TruckId { get; init; }

    public string? TruckCode { get; init; }

    public int? DriverId { get; init; }

    public string? DriverCode { get; init; }

    public string? DriverDisplayName { get; init; }

    public int? WorkShiftId { get; init; }

    public string? WorkShiftCode { get; init; }

    public string? WorkShiftName { get; init; }

    public int? LoadingPointId { get; init; }

    public string? LoadingPointCode { get; init; }

    public string? LoadingPointName { get; init; }

    public int? DestinationId { get; init; }

    public string? DestinationCode { get; init; }

    public string? DestinationName { get; init; }
}
