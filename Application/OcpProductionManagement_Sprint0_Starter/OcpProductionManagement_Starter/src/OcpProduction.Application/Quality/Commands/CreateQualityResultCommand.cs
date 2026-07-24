namespace OcpProduction.Application.Quality.Commands;

public sealed class CreateQualityResultCommand
{
    public DateTime AnalysisDate { get; init; }

    public long? TripId { get; init; }

    public int? LoadingPointId { get; init; }

    public decimal QualityValue { get; init; }

    public string? Comments { get; init; }
}
