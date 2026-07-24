namespace OcpProduction.Application.Dashboard.Dtos;

public sealed class DashboardChartDataDto
{
    public IReadOnlyList<DailyTripTonnagePointDto> DailyTripsAndTonnage { get; init; } = [];

    public IReadOnlyList<TripStatusPointDto> TripsByStatus { get; init; } = [];

    public IReadOnlyList<StopReasonDurationPointDto> StopDurationByReason { get; init; } = [];

    public IReadOnlyList<TruckActivityPointDto> TruckActivity { get; init; } = [];

    public IReadOnlyList<P2O5TrendPointDto> P2O5Trend { get; init; } = [];
}

public sealed class DailyTripTonnagePointDto
{
    public DateOnly Date { get; init; }

    public long TripCount { get; init; }

    public decimal CompletedTonnage { get; init; }
}

public sealed class TripStatusPointDto
{
    public string Status { get; init; } = string.Empty;

    public long TripCount { get; init; }
}

public sealed class StopReasonDurationPointDto
{
    public int StopReasonId { get; init; }

    public string StopReasonName { get; init; } = string.Empty;

    public long StopCount { get; init; }

    public long DurationMinutes { get; init; }

    public decimal DurationHours => DurationMinutes / 60m;
}

public sealed class TruckActivityPointDto
{
    public int TruckId { get; init; }

    public string TruckCode { get; init; } = string.Empty;

    public long TripCount { get; init; }
}

public sealed class P2O5TrendPointDto
{
    public DateOnly Date { get; init; }

    public decimal AverageP2O5 { get; init; }

    public long MeasurementCount { get; init; }
}
