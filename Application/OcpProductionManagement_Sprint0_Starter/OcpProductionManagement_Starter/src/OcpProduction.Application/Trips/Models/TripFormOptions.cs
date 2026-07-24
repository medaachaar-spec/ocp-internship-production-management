namespace OcpProduction.Application.Trips.Models;

public sealed record TripFormOption(int Value, string Label);

public sealed class TripFormOptions
{
    public IReadOnlyList<TripFormOption> Trucks { get; init; } = [];

    public IReadOnlyList<TripFormOption> Drivers { get; init; } = [];

    public IReadOnlyList<TripFormOption> WorkShifts { get; init; } = [];

    public IReadOnlyList<TripFormOption> LoadingPoints { get; init; } = [];

    public IReadOnlyList<TripFormOption> Destinations { get; init; } = [];
}
