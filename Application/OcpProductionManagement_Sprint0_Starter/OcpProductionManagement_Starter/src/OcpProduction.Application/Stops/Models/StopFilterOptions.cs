namespace OcpProduction.Application.Stops.Models;

public sealed record StopFilterOption(int Value, string Label);

public sealed class StopFilterOptions
{
    public IReadOnlyList<StopFilterOption> Trucks { get; init; } = [];

    public IReadOnlyList<StopFilterOption> StopReasons { get; init; } = [];

    public IReadOnlyList<string> States { get; init; } = StopStates.All;

    public IReadOnlyList<string> ValidationStatuses { get; init; } = [];
}
