namespace OcpProduction.Application.Drivers.Models;

public sealed class DriverFilterOptions
{
    public IReadOnlyList<string> Statuses { get; init; } = [];
}
