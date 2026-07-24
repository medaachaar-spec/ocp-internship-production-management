namespace OcpProduction.Application.Quality.Commands;

public sealed class UpdateQualityResultCommand
{
    public long QualityResultId { get; init; }

    public DateTime AnalysisDate { get; init; }

    public long? TripId { get; init; }

    public int? LoadingPointId { get; init; }

    public decimal QualityValue { get; init; }

    public string? Comments { get; init; }
}
