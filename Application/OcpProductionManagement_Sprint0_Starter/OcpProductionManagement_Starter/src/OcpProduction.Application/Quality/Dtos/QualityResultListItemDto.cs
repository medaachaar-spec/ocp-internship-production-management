namespace OcpProduction.Application.Quality.Dtos;

public sealed class QualityResultListItemDto
{
    public long QualityResultId { get; init; }

    public long? TripId { get; init; }

    public DateTime AnalysisDate { get; init; }

    public decimal? P2O5Value { get; init; }

    public string QualityUnit { get; init; } = string.Empty;

    public int? TruckId { get; init; }

    public string? TruckCode { get; init; }

    public string? LoadingPointCode { get; init; }

    public string? LoadingPointName { get; init; }

    public string? DestinationCode { get; init; }

    public string? DestinationName { get; init; }

    public string Status { get; init; } = string.Empty;
}
