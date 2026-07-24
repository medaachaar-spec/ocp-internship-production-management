namespace OcpProduction.Application.Trucks.Models;

public sealed class TruckFilterOptions
{
    public IReadOnlyList<string> Statuses { get; init; } = [];
    public IReadOnlyList<string> Models { get; init; } = [];
}
