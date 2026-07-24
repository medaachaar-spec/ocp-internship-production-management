namespace OcpProduction.Application.Quality.Models;

public sealed record QualityFilterOption(int Value, string Label);

public sealed class QualityFilterOptions
{
    public IReadOnlyList<QualityFilterOption> Trucks { get; init; } = [];

    public IReadOnlyList<string> Statuses { get; init; } = [];
}
