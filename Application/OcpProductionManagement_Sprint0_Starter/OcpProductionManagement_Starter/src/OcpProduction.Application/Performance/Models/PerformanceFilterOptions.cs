namespace OcpProduction.Application.Performance.Models;

public sealed class PerformanceFilterOptions
{
    public IReadOnlyList<string> TruckStatuses { get; init; } = [];
    public IReadOnlyList<string> DriverStatuses { get; init; } = [];
}
