using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Web.ViewModels.Dashboard;
using System.Text.Json;
using Xunit;

namespace OcpProduction.Tests.Dashboard;

public sealed class DashboardPresentationTests
{
    [Fact]
    public void Criteria_identifies_only_reversed_ranges_as_invalid()
    {
        Assert.False(new DashboardDateCriteria().HasInvalidRange);
        Assert.False(new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 6, 1)
        }.HasInvalidRange);
        Assert.False(new DashboardDateCriteria
        {
            EndDate = new DateOnly(2026, 6, 30)
        }.HasInvalidRange);
        Assert.False(new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 6, 30),
            EndDate = new DateOnly(2026, 6, 30)
        }.HasInvalidRange);
        Assert.True(new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 6, 30)
        }.HasInvalidRange);
    }

    [Fact]
    public void View_model_exposes_factual_inclusive_period_labels()
    {
        Assert.Equal(
            "Toutes les données",
            new DashboardViewModel().PeriodLabel);
        Assert.Equal(
            "À partir du 01/06/2026 inclus",
            new DashboardViewModel
            {
                Criteria = new DashboardDateCriteria
                {
                    StartDate = new DateOnly(2026, 6, 1)
                }
            }.PeriodLabel);
        Assert.Equal(
            "Jusqu’au 30/06/2026 inclus",
            new DashboardViewModel
            {
                Criteria = new DashboardDateCriteria
                {
                    EndDate = new DateOnly(2026, 6, 30)
                }
            }.PeriodLabel);
        Assert.Equal(
            "Du 01/06/2026 au 30/06/2026 inclus",
            new DashboardViewModel
            {
                Criteria = new DashboardDateCriteria
                {
                    StartDate = new DateOnly(2026, 6, 1),
                    EndDate = new DateOnly(2026, 6, 30)
                }
            }.PeriodLabel);
    }

    [Fact]
    public void Empty_and_nonempty_dashboard_states_are_distinct()
    {
        Assert.False(new DashboardViewModel().HasAnyData);
        Assert.True(new DashboardViewModel
        {
            Kpis = new DashboardDto { StopCount = 1 }
        }.HasAnyData);
    }

    [Fact]
    public void Razor_page_contains_six_factual_cards_five_charts_and_accessible_fallbacks()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Dashboard", "Index.cshtml"));

        Assert.Contains("Tableau de bord de production", source, StringComparison.Ordinal);
        Assert.Contains("Nombre de voyages", source, StringComparison.Ordinal);
        Assert.Contains("Tonnage total", source, StringComparison.Ordinal);
        Assert.Contains("Nombre d'arrêts", source, StringComparison.Ordinal);
        Assert.Contains("Durée totale des arrêts", source, StringComparison.Ordinal);
        Assert.Contains("Nombre d'analyses qualité", source, StringComparison.Ordinal);
        Assert.Contains("Voyages en attente de validation", source, StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.TripCount.ToString(\"N0\")",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.TotalTonnage.ToString(\"N2\") t",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.StopCount.ToString(\"N0\")",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.TotalStopDurationHours.ToString(\"N2\") h",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.QualityAnalysisCount.ToString(\"N0\")",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "@Model.Kpis.PendingValidationTripCount.ToString(\"N0\")",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "Les données du tableau de bord ne peuvent pas être chargées actuellement.",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "Aucune donnée n’est disponible pour la période sélectionnée.",
            source,
            StringComparison.Ordinal);
        Assert.Contains("Voyages et tonnage par jour", source, StringComparison.Ordinal);
        Assert.Contains("Répartition des voyages par statut", source, StringComparison.Ordinal);
        Assert.Contains("Durée des arrêts par motif", source, StringComparison.Ordinal);
        Assert.Contains("Nombre de voyages par camion", source, StringComparison.Ordinal);
        Assert.Contains("Évolution du P2O5", source, StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(source, "<canvas"));
        Assert.Equal(5, CountOccurrences(source, "dashboard-chart-fallback"));
        Assert.Contains("role=\"img\"", source, StringComparison.Ordinal);
        Assert.Contains(
            "Aucune donnée P2O5 pour la période sélectionnée.",
            source,
            StringComparison.Ordinal);
        Assert.Contains("type=\"application/json\"", source, StringComparison.Ordinal);
        Assert.Contains("@Html.Raw(Model.ChartDataJson)", source, StringComparison.Ordinal);
        Assert.Contains("chart.js@4.4.7", source, StringComparison.Ordinal);
        Assert.Contains("~/js/dashboard.js", source, StringComparison.Ordinal);
        Assert.Contains("@section Scripts", source, StringComparison.Ordinal);
        Assert.Contains("dashboard-status-legend", source, StringComparison.Ordinal);
        Assert.Contains("@point.Status", source, StringComparison.Ordinal);
        Assert.Contains(
            "point.TripCount * 100m / statusTripTotal",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain("arrêt exceptionnel", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("133 933", source, StringComparison.Ordinal);
        Assert.Contains("Moyenne de la période :", source, StringComparison.Ordinal);
        Assert.Contains(
            "point.AverageP2O5 * point.MeasurementCount",
            source,
            StringComparison.Ordinal);
        Assert.Contains("/ p2o5MeasurementTotal", source, StringComparison.Ordinal);

        AssertConditionalCanvas(source, "Model.HasDailyTripsAndTonnage", "daily-trips-chart");
        AssertConditionalCanvas(source, "Model.HasTripsByStatus", "trip-status-chart");
        AssertConditionalCanvas(source, "Model.HasStopDurationByReason", "stop-reason-chart");
        AssertConditionalCanvas(source, "Model.HasTruckActivity", "truck-activity-chart");
        AssertConditionalCanvas(source, "Model.HasP2O5Trend", "p2o5-trend-chart");
    }

    [Fact]
    public void View_model_maps_all_chart_series_and_serializes_script_safe_json()
    {
        const string maliciousLabel = "</script><script>alert('x')</script>";
        var charts = new DashboardChartDataDto
        {
            DailyTripsAndTonnage =
            [
                new()
                {
                    Date = new DateOnly(2026, 6, 1),
                    TripCount = 25,
                    CompletedTonnage = 1_234.50m
                }
            ],
            TripsByStatus =
            [
                new() { Status = maliciousLabel, TripCount = 25 }
            ],
            StopDurationByReason =
            [
                new()
                {
                    StopReasonId = 2,
                    StopReasonName = "Panne électrique",
                    StopCount = 3,
                    DurationMinutes = 180
                }
            ],
            TruckActivity =
            [
                new()
                {
                    TruckId = 12,
                    TruckCode = "TRK-012",
                    TripCount = 25
                }
            ],
            P2O5Trend =
            [
                new()
                {
                    Date = new DateOnly(2026, 6, 1),
                    AverageP2O5 = 29.5475m,
                    MeasurementCount = 4
                }
            ]
        };
        var model = new DashboardViewModel
        {
            Kpis = new DashboardDto { Charts = charts }
        };

        Assert.True(model.HasDailyTripsAndTonnage);
        Assert.True(model.HasTripsByStatus);
        Assert.True(model.HasStopDurationByReason);
        Assert.True(model.HasTruckActivity);
        Assert.True(model.HasP2O5Trend);
        Assert.True(model.HasAnyChartData);

        var json = model.ChartDataJson;

        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C", json, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("dailyTripsAndTonnage", out _));
        Assert.True(root.TryGetProperty("tripsByStatus", out var statuses));
        Assert.True(root.TryGetProperty("stopDurationByReason", out _));
        Assert.True(root.TryGetProperty("truckActivity", out _));
        Assert.True(root.TryGetProperty("p2O5Trend", out _));
        Assert.Equal(maliciousLabel, statuses[0].GetProperty("status").GetString());
    }

    [Fact]
    public void Empty_chart_data_is_supported_without_false_availability()
    {
        var model = new DashboardViewModel
        {
            Kpis = new DashboardDto()
        };

        Assert.False(model.HasDailyTripsAndTonnage);
        Assert.False(model.HasTripsByStatus);
        Assert.False(model.HasStopDurationByReason);
        Assert.False(model.HasTruckActivity);
        Assert.False(model.HasP2O5Trend);
        Assert.False(model.HasAnyChartData);
    }

    [Fact]
    public void Dashboard_scripts_are_page_scoped_resilient_and_free_of_business_judgments()
    {
        var viewSource = File.ReadAllText(GetWebFile("Views", "Dashboard", "Index.cshtml"));
        var layoutSource = File.ReadAllText(GetWebFile("Views", "Shared", "_Layout.cshtml"));
        var scriptSource = File.ReadAllText(GetWebFile("wwwroot", "js", "dashboard.js"));
        var cssSource = File.ReadAllText(GetWebFile("wwwroot", "css", "site.css"));
        var dashboardCss = cssSource[cssSource.IndexOf(".dashboard-page", StringComparison.Ordinal)..];

        Assert.DoesNotContain("chart.js", layoutSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("window.Chart", scriptSource, StringComparison.Ordinal);
        Assert.Contains("JSON.parse(dataElement.textContent)", scriptSource, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", scriptSource, StringComparison.Ordinal);
        Assert.Contains("beginAtZero: true", scriptSource, StringComparison.Ordinal);
        Assert.Contains("fallback?.removeAttribute(\"open\")", scriptSource, StringComparison.Ordinal);
        Assert.Contains("visual.hidden = false", scriptSource, StringComparison.Ordinal);
        Assert.Contains("type: \"doughnut\"", scriptSource, StringComparison.Ordinal);
        Assert.Contains("statuses.map(point => point.status)", scriptSource, StringComparison.Ordinal);
        Assert.Contains("statuses.reduce(", scriptSource, StringComparison.Ordinal);
        Assert.Contains("formatPercentage(count, total)", scriptSource, StringComparison.Ordinal);
        Assert.Contains("dashboardStatusCenterText", scriptSource, StringComparison.Ordinal);
        Assert.Contains(
            "context.fillText(total === 1 ? \"voyage\" : \"voyages\"",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains("percentageFormatter", scriptSource, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(scriptSource, "autoSkip: true"));
        Assert.Equal(2, CountOccurrences(scriptSource, "maxTicksLimit: 12"));
        Assert.Contains(
            "daily.map(point => formatDate(point.date))",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "daily.map(point => point.tripCount)",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "daily.map(point => point.completedTonnage)",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "p2o5.map(point => formatDate(point.date))",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "p2o5.map(point => point.averageP2O5)",
            scriptSource,
            StringComparison.Ordinal);
        Assert.Contains("dashboardBarValueLabels", scriptSource, StringComparison.Ordinal);
        Assert.Contains("chart.width < 560", scriptSource, StringComparison.Ordinal);
        Assert.Contains("point.stopCount", scriptSource, StringComparison.Ordinal);
        Assert.Contains("trucks.map(point => point.tripCount)", scriptSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ChartDataLabels", scriptSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chartjs-plugin", scriptSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@section Scripts", viewSource, StringComparison.Ordinal);
        Assert.Contains("@media (max-width:", dashboardCss, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", dashboardCss, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", dashboardCss, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%", dashboardCss, StringComparison.Ordinal);
        Assert.Contains(".dashboard-filter-actions .btn", dashboardCss, StringComparison.Ordinal);
        Assert.Contains(".dashboard-chart-visual-reasons", dashboardCss, StringComparison.Ordinal);
        Assert.Contains(".dashboard-chart-visual-trucks", dashboardCss, StringComparison.Ordinal);
        Assert.DoesNotContain("gradient", dashboardCss, StringComparison.OrdinalIgnoreCase);

        var presentationSource = string.Join(Environment.NewLine, viewSource, scriptSource, dashboardCss);
        foreach (var forbiddenTerm in new[]
                 {
                     "best", "worst", "ranking", "score", "performance", "threshold",
                     "target", "conform", "efficient", "inefficient", "traffic-light",
                     "meilleur", "pire", "classement", "seuil", "objectif"
                 })
        {
            Assert.DoesNotContain(
                forbiddenTerm,
                presentationSource,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Sidebar_dashboard_link_uses_dashboard_policy_without_expanding_reporting_links()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Shared", "_Sidebar.cshtml"));
        var dashboardPolicyIndex = source.IndexOf(
            "PolicyNames.CanViewDashboard",
            StringComparison.Ordinal);
        var reportingPolicyIndex = source.IndexOf(
            "PolicyNames.CanViewReporting",
            StringComparison.Ordinal);
        var dashboardGateIndex = source.IndexOf(
            "@if (canViewDashboard)",
            StringComparison.Ordinal);
        var reportingGateIndex = source.IndexOf(
            "@if (canViewReporting)",
            StringComparison.Ordinal);
        var dashboardLinkIndex = source.IndexOf(
            "asp-controller=\"Dashboard\"",
            StringComparison.Ordinal);
        var performanceLinkIndex = source.IndexOf(
            "asp-controller=\"Performance\"",
            StringComparison.Ordinal);

        Assert.True(dashboardPolicyIndex >= 0);
        Assert.True(reportingPolicyIndex >= 0);
        Assert.True(dashboardGateIndex > dashboardPolicyIndex);
        Assert.True(dashboardLinkIndex > dashboardGateIndex);
        Assert.True(reportingGateIndex > dashboardLinkIndex);
        Assert.True(performanceLinkIndex > reportingGateIndex);
        Assert.Equal(1, CountOccurrences(source, "asp-controller=\"Dashboard\""));
    }

    [Fact]
    public void Dashboard_query_service_is_explicitly_read_only_and_uses_detailed_views()
    {
        var source = File.ReadAllText(GetSolutionFile(
            "src",
            "OcpProduction.Infrastructure",
            "Dashboard",
            "DashboardReadService.cs"));

        Assert.Contains("vw_TripSummaries.AsNoTracking()", source, StringComparison.Ordinal);
        Assert.Contains("vw_StopAnalyses.AsNoTracking()", source, StringComparison.Ordinal);
        Assert.Contains("vw_QualityAnalyses.AsNoTracking()", source, StringComparison.Ordinal);
        Assert.Contains("TripStatuses.Completed", source, StringComparison.Ordinal);
        Assert.Contains("TripStatuses.PendingValidation", source, StringComparison.Ordinal);
        Assert.Contains("/ 60m", source, StringComparison.Ordinal);
        Assert.Contains(".GroupBy(trip => trip.TripDate)", source, StringComparison.Ordinal);
        Assert.Contains(
            ".GroupBy(trip => new { trip.TruckID, trip.TruckCode })",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            ".GroupBy(stop => new { stop.StopReasonID, stop.StopReasonName })",
            source,
            StringComparison.Ordinal);
        Assert.Contains(".GroupBy(result => result.QualityDate)", source, StringComparison.Ordinal);
        Assert.Contains("AverageP2O5", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vw_ExecutiveDashboard", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vw_GlobalProductionSummary", source, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;

        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static void AssertConditionalCanvas(string source, string condition, string canvasId)
    {
        var conditionIndex = source.IndexOf(condition, StringComparison.Ordinal);
        var canvasIndex = source.IndexOf($"id=\"{canvasId}\"", StringComparison.Ordinal);

        Assert.True(conditionIndex >= 0, $"The condition '{condition}' was not found.");
        Assert.True(canvasIndex > conditionIndex, $"The canvas '{canvasId}' is not guarded by '{condition}'.");
    }

    private static string GetWebFile(params string[] segments)
    {
        return GetSolutionFile(
            ["src", "OcpProduction.Web", .. segments]);
    }

    private static string GetSolutionFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OcpProductionManagement.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, Path.Combine(segments));
    }
}
