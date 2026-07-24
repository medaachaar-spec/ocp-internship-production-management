namespace OcpProduction.Application.Stops.Models;

public sealed record StopFormOption(int Value, string Label);

public sealed class StopFormOptions
{
    public IReadOnlyList<StopFormOption> Trucks { get; init; } = [];

    public IReadOnlyList<StopFormOption> Drivers { get; init; } = [];

    public IReadOnlyList<StopFormOption> WorkShifts { get; init; } = [];

    public IReadOnlyList<StopFormOption> StopReasons { get; init; } = [];
}
