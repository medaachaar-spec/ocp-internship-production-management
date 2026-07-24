namespace OcpProduction.Application.Quality.Models;

public sealed record QualityLoadingPointOption(int Value, string Label);

public sealed class QualityFormOptions
{
    public IReadOnlyList<QualityLoadingPointOption> LoadingPoints { get; init; } = [];
}
