using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Performance.Dtos;

public sealed class CategoryLongValueDto
{
    public int EntityId { get; init; }
    public string Category { get; init; } = string.Empty;
    public long Value { get; init; }
}

public sealed class CategoryDecimalValueDto
{
    public int EntityId { get; init; }
    public string Category { get; init; } = string.Empty;
    public decimal Value { get; init; }
}

public sealed class DriverTripChartPointDto
{
    public int DriverId { get; init; }
    public string Category { get; init; } = string.Empty;
    public long TotalTrips { get; init; }
    public long CompletedTrips { get; init; }
}

public sealed class TruckPerformanceSummaryDto
{
    public int DisplayedTruckCount { get; init; }
    public long TripCount { get; init; }
    public decimal CompletedTonnage { get; init; }
    public long StopCount { get; init; }
    public long TotalStopDurationMinutes { get; init; }
}

public sealed class DriverPerformanceSummaryDto
{
    public int DisplayedDriverCount { get; init; }
    public long TripCount { get; init; }
    public long CompletedTripCount { get; init; }
    public decimal CompletedTonnage { get; init; }
    public long LinkedStopCount { get; init; }
}

public sealed class TruckPerformanceChartDataDto
{
    public IReadOnlyList<CategoryLongValueDto> TripsByTruck { get; init; } = [];
    public IReadOnlyList<CategoryDecimalValueDto> TonnageByTruck { get; init; } = [];
    public IReadOnlyList<CategoryLongValueDto> StopsByTruck { get; init; } = [];
    public IReadOnlyList<CategoryDecimalValueDto> P2O5ByTruck { get; init; } = [];
}

public sealed class DriverPerformanceChartDataDto
{
    public IReadOnlyList<DriverTripChartPointDto> TripsByDriver { get; init; } = [];
    public IReadOnlyList<CategoryDecimalValueDto> TonnageByDriver { get; init; } = [];
    public IReadOnlyList<CategoryLongValueDto> LinkedStopsByDriver { get; init; } = [];
}

public sealed class TruckPerformanceReportDto
{
    public IReadOnlyList<TruckPerformanceListItemDto> FilteredItems { get; init; } = [];
    public PagedResult<TruckPerformanceListItemDto> TableRows { get; init; } =
        PagedResult<TruckPerformanceListItemDto>.Create([], 1, 20, 0);
    public TruckPerformanceSummaryDto Summary { get; init; } = new();
    public TruckPerformanceChartDataDto Charts { get; init; } = new();
}

public sealed class DriverPerformanceReportDto
{
    public IReadOnlyList<DriverPerformanceListItemDto> FilteredItems { get; init; } = [];
    public PagedResult<DriverPerformanceListItemDto> TableRows { get; init; } =
        PagedResult<DriverPerformanceListItemDto>.Create([], 1, 20, 0);
    public DriverPerformanceSummaryDto Summary { get; init; } = new();
    public DriverPerformanceChartDataDto Charts { get; init; } = new();
}
