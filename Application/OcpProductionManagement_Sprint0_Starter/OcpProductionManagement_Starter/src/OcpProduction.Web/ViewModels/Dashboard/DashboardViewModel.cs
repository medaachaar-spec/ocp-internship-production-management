using System.Text.Json;
using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;

namespace OcpProduction.Web.ViewModels.Dashboard;

public sealed class DashboardViewModel
{
    private static readonly JsonSerializerOptions ChartJsonOptions =
        new(JsonSerializerDefaults.Web);

    public DashboardDateCriteria Criteria { get; init; } = new();

    public DashboardDto Kpis { get; init; } = new();

    public bool HasLoadError { get; init; }

    public bool HasAnyData =>
        Kpis.TripCount > 0 ||
        Kpis.StopCount > 0 ||
        Kpis.QualityAnalysisCount > 0;

    public bool HasDailyTripsAndTonnage =>
        Kpis.Charts.DailyTripsAndTonnage.Count > 0;

    public bool HasTripsByStatus =>
        Kpis.Charts.TripsByStatus.Count > 0;

    public bool HasStopDurationByReason =>
        Kpis.Charts.StopDurationByReason.Count > 0;

    public bool HasTruckActivity =>
        Kpis.Charts.TruckActivity.Count > 0;

    public bool HasP2O5Trend =>
        Kpis.Charts.P2O5Trend.Count > 0;

    public bool HasAnyChartData =>
        HasDailyTripsAndTonnage ||
        HasTripsByStatus ||
        HasStopDurationByReason ||
        HasTruckActivity ||
        HasP2O5Trend;

    public string ChartDataJson =>
        JsonSerializer.Serialize(Kpis.Charts, ChartJsonOptions);

    public string PeriodLabel => (Criteria.StartDate, Criteria.EndDate) switch
    {
        (null, null) => "Toutes les données",
        ({ } startDate, null) => $"À partir du {startDate:dd/MM/yyyy} inclus",
        (null, { } endDate) => $"Jusqu’au {endDate:dd/MM/yyyy} inclus",
        ({ } startDate, { } endDate) => $"Du {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy} inclus"
    };
}
