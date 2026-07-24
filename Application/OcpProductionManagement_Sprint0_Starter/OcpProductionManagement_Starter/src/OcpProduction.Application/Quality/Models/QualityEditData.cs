namespace OcpProduction.Application.Quality.Models;

public sealed class QualityEditData
{
    public long QualityResultId { get; init; }

    public DateTime AnalysisDate { get; init; }

    public long? TripId { get; init; }

    public int? LoadingPointId { get; init; }

    public decimal QualityValue { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? Comments { get; init; }
}
