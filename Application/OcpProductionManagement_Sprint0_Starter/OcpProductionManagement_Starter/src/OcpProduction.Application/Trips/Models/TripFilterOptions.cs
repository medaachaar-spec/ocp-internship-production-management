namespace OcpProduction.Application.Trips.Models;

public sealed record TripFilterOption(int Value, string Label);

public sealed class TripFilterOptions
{
    public IReadOnlyList<TripFilterOption> Trucks { get; init; } = [];

    public IReadOnlyList<TripFilterOption> Drivers { get; init; } = [];

    public IReadOnlyList<TripFilterOption> WorkShifts { get; init; } = [];

    public IReadOnlyList<string> Statuses { get; init; } = [];
}
