namespace OcpProduction.Application.Dashboard.Dtos;

public sealed class DashboardDto
{
    public long TripCount { get; init; }

    public decimal TotalTonnage { get; init; }

    public long StopCount { get; init; }

    public decimal TotalStopDurationHours { get; init; }

    public long QualityAnalysisCount { get; init; }

    public long PendingValidationTripCount { get; init; }

    public DashboardChartDataDto Charts { get; init; } = new();
}
