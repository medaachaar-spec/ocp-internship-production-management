using System.Runtime.CompilerServices;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformancePresentationTests
{
    [Fact]
    public void Truck_page_preserves_controls_table_links_and_adds_accessible_charts()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Performance", "Trucks.cshtml"));

        Assert.Contains("Performance camions", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Search\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Status\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Sort\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"PageSize\"", source, StringComparison.Ordinal);
        Assert.Contains("Réinitialiser", source, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"TruckDetails\"", source, StringComparison.Ordinal);
        Assert.Contains("Pagination des performances camions", source, StringComparison.Ordinal);
        Assert.Contains("Détail par camion", source, StringComparison.Ordinal);
        Assert.Contains("indépendamment de la page du tableau", source, StringComparison.Ordinal);
        Assert.Contains("Nombre de voyages par camion", source, StringComparison.Ordinal);
        Assert.Contains("Tonnage transporté par camion", source, StringComparison.Ordinal);
        Assert.Contains("Nombre d’arrêts par camion", source, StringComparison.Ordinal);
        Assert.Contains("P2O5 moyen observé par camion", source, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(source, "<canvas"));
        Assert.Equal(4, CountOccurrences(source, "performance-chart-fallback"));
        Assert.Equal(4, CountOccurrences(source, "<caption"));
        Assert.Contains("role=\"img\"", source, StringComparison.Ordinal);
        Assert.Contains("type=\"application/json\"", source, StringComparison.Ordinal);
        Assert.Contains("@Html.Raw(Model.ChartDataJson)", source, StringComparison.Ordinal);
        Assert.Contains("chart.js@4.4.7", source, StringComparison.Ordinal);
        Assert.Contains("~/js/chart-theme.js", source, StringComparison.Ordinal);
        Assert.Contains("~/js/truck-performance.js", source, StringComparison.Ordinal);
        Assert.Contains("Les données de performance des camions ne peuvent pas être chargées actuellement.", source, StringComparison.Ordinal);
        Assert.Contains("Aucune donnée de voyage pour les filtres sélectionnés.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("seuil", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("objectif", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("classement", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meilleur", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pire", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vw_TruckPerformance", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Driver_page_preserves_controls_table_links_and_uses_association_wording()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Performance", "Drivers.cshtml"));

        Assert.Contains("Performance conducteurs", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Search\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Status\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"Sort\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"PageSize\"", source, StringComparison.Ordinal);
        Assert.Contains("Réinitialiser", source, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"DriverDetails\"", source, StringComparison.Ordinal);
        Assert.Contains("Pagination des performances conducteurs", source, StringComparison.Ordinal);
        Assert.Contains("Détail par conducteur", source, StringComparison.Ordinal);
        Assert.Contains("indépendamment de la page du tableau", source, StringComparison.Ordinal);
        Assert.Contains("Voyages par conducteur", source, StringComparison.Ordinal);
        Assert.Contains("Tonnage transporté par conducteur", source, StringComparison.Ordinal);
        Assert.Contains("Arrêts associés par conducteur", source, StringComparison.Ordinal);
        Assert.Contains("n’attribue ni cause ni responsabilité", source, StringComparison.Ordinal);
        Assert.Equal(3, CountOccurrences(source, "<canvas"));
        Assert.Equal(3, CountOccurrences(source, "performance-chart-fallback"));
        Assert.Equal(3, CountOccurrences(source, "<caption"));
        Assert.Contains("role=\"img\"", source, StringComparison.Ordinal);
        Assert.Contains("chart.js@4.4.7", source, StringComparison.Ordinal);
        Assert.Contains("~/js/chart-theme.js", source, StringComparison.Ordinal);
        Assert.Contains("~/js/driver-performance.js", source, StringComparison.Ordinal);
        Assert.DoesNotContain("P2O5 moyen associé aux voyages du conducteur", source, StringComparison.Ordinal);
        Assert.DoesNotContain("faute", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("responsable", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("classement", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meilleur", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pire", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Truck_detail_preserves_all_metrics_return_filters_and_neutral_wording()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Performance", "TruckDetails.cshtml"));

        Assert.Contains("performance-detail-header", source, StringComparison.Ordinal);
        Assert.Contains("Performance camion", source, StringComparison.Ordinal);
        Assert.Contains("Indicateurs consolidés à partir de l’historique disponible.", source, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(source, "performance-detail-metric__label"));
        Assert.Equal(4, CountOccurrences(source, "performance-detail-metric__value"));
        Assert.Contains("<dl class=\"performance-detail-facts", source, StringComparison.Ordinal);
        Assert.Contains("<dt>", source, StringComparison.Ordinal);
        Assert.Contains("<dd>", source, StringComparison.Ordinal);
        Assert.Contains("Période analysée", source, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Trucks\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Search=\"@Model.ReturnCriteria.Search\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Status=\"@Model.ReturnCriteria.Status\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Sort=\"@Model.ReturnCriteria.Sort\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Page=\"@Model.ReturnCriteria.Page\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-PageSize=\"@Model.ReturnCriteria.PageSize\"", source, StringComparison.Ordinal);

        foreach (var property in new[]
                 {
                     "TotalTrips", "TotalTonnage", "CompletionRatePercent", "TotalStopCount",
                     "CompletedTrips", "IncompleteTrips", "RatedPayloadTonnes", "AveragePayloadTonnes",
                     "AveragePayloadUtilizationPercent", "AverageTripDurationMinutes", "CompletedStopCount",
                     "TotalDowntimeMinutes", "AverageStopDurationMinutes", "QualityResultCount", "AverageP2O5",
                     "FirstTripDate", "LastTripDate"
                 })
        {
            Assert.Contains($"performance.{property}", source, StringComparison.Ordinal);
        }

        Assert.Contains("@section Styles", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vw_TruckPerformance", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("classement", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meilleur", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mauvais", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Driver_detail_preserves_all_metrics_return_filters_and_association_wording()
    {
        var source = File.ReadAllText(GetWebFile("Views", "Performance", "DriverDetails.cshtml"));

        Assert.Contains("performance-detail-header", source, StringComparison.Ordinal);
        Assert.Contains("Performance conducteur", source, StringComparison.Ordinal);
        Assert.Contains("Indicateurs consolidés à partir de l’historique disponible.", source, StringComparison.Ordinal);
        Assert.Contains("Arrêts associés", source, StringComparison.Ordinal);
        Assert.Contains("Voyages avec arrêts associés", source, StringComparison.Ordinal);
        Assert.Contains("P2O5 moyen associé aux voyages", source, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(source, "performance-detail-metric__label"));
        Assert.Equal(4, CountOccurrences(source, "performance-detail-metric__value"));
        Assert.Contains("<dl class=\"performance-detail-facts", source, StringComparison.Ordinal);
        Assert.Contains("<dt>", source, StringComparison.Ordinal);
        Assert.Contains("<dd>", source, StringComparison.Ordinal);
        Assert.Contains("Période analysée", source, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Drivers\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Search=\"@Model.ReturnCriteria.Search\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Status=\"@Model.ReturnCriteria.Status\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Sort=\"@Model.ReturnCriteria.Sort\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-Page=\"@Model.ReturnCriteria.Page\"", source, StringComparison.Ordinal);
        Assert.Contains("asp-route-PageSize=\"@Model.ReturnCriteria.PageSize\"", source, StringComparison.Ordinal);

        foreach (var property in new[]
                 {
                     "TotalTrips", "TotalTonnage", "CompletionRatePercent", "TotalLinkedStopCount",
                     "CompletedTrips", "IncompleteTrips", "AveragePayloadTonnes", "AveragePayloadUtilizationPercent",
                     "AverageTripDurationMinutes", "TripsWithStops", "TotalLinkedStopDurationMinutes",
                     "AverageLinkedStopDurationMinutes", "QualityResultCount", "AverageP2O5", "MinimumP2O5",
                     "MaximumP2O5", "FirstTripDate", "LastTripDate"
                 })
        {
            Assert.Contains($"performance.{property}", source, StringComparison.Ordinal);
        }

        Assert.Contains("@section Styles", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vw_DriverPerformance", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("faute", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meilleur", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mauvais", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score conducteur", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("qualité du conducteur", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Performance_assets_are_page_scoped_resilient_responsive_and_read_only()
    {
        var layout = File.ReadAllText(GetWebFile("Views", "Shared", "_Layout.cshtml"));
        var truckScript = File.ReadAllText(GetWebFile("wwwroot", "js", "truck-performance.js"));
        var driverScript = File.ReadAllText(GetWebFile("wwwroot", "js", "driver-performance.js"));
        var chartTheme = File.ReadAllText(GetWebFile("wwwroot", "js", "chart-theme.js"));
        var styles = File.ReadAllText(GetWebFile("wwwroot", "css", "performance.css"));
        var service = File.ReadAllText(GetSolutionFile(
            "src",
            "OcpProduction.Infrastructure",
            "Performance",
            "PerformanceReadService.cs"));

        Assert.DoesNotContain("chart.js", layout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RenderSectionAsync(\"Styles\"", layout, StringComparison.Ordinal);
        Assert.Contains("typeof window.Chart", truckScript, StringComparison.Ordinal);
        Assert.Contains("typeof window.Chart", driverScript, StringComparison.Ordinal);
        Assert.Contains("window.OcpChartTheme", truckScript, StringComparison.Ordinal);
        Assert.Contains("window.OcpChartTheme", driverScript, StringComparison.Ordinal);
        Assert.Contains("JSON.parse(dataElement.textContent)", truckScript, StringComparison.Ordinal);
        Assert.Contains("JSON.parse(dataElement.textContent)", driverScript, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", truckScript, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", driverScript, StringComparison.Ordinal);
        Assert.Contains("visual.hidden = true", truckScript, StringComparison.Ordinal);
        Assert.Contains("visual.hidden = true", driverScript, StringComparison.Ordinal);
        Assert.Contains("autoSkip: false", truckScript, StringComparison.Ordinal);
        Assert.Contains("autoSkip: false", driverScript, StringComparison.Ordinal);
        Assert.Contains("theme.tooltip(", truckScript, StringComparison.Ordinal);
        Assert.Contains("theme.tooltip(", driverScript, StringComparison.Ordinal);
        Assert.Contains("theme.barValueLabelsPlugin", truckScript, StringComparison.Ordinal);
        Assert.Contains("theme.barValueLabelsPlugin", driverScript, StringComparison.Ordinal);
        Assert.Contains("greenFill: \"#b9d8c8\"", chartTheme, StringComparison.Ordinal);
        Assert.Contains("blueGrayFill: \"#bed1da\"", chartTheme, StringComparison.Ordinal);
        Assert.Contains("beigeFill: \"#dfd0b9\"", chartTheme, StringComparison.Ordinal);
        Assert.Contains("borderWidth: 2", truckScript, StringComparison.Ordinal);
        Assert.Contains("borderWidth: 2", driverScript, StringComparison.Ordinal);
        Assert.DoesNotContain("const colors = {", truckScript, StringComparison.Ordinal);
        Assert.DoesNotContain("const colors = {", driverScript, StringComparison.Ordinal);
        Assert.DoesNotContain("devicePixelRatio", chartTheme, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1199.98px)", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 767.98px)", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 575.98px)", styles, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", styles, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", styles, StringComparison.Ordinal);
        Assert.Contains(".performance-chart-visual-compact", styles, StringComparison.Ordinal);
        Assert.Contains(".performance-detail-header", styles, StringComparison.Ordinal);
        Assert.Contains(".performance-detail-metric-grid", styles, StringComparison.Ordinal);
        Assert.Contains(".performance-detail-facts", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(4", styles, StringComparison.Ordinal);
        Assert.Contains("min-width: 0", styles, StringComparison.Ordinal);
        Assert.Contains("width: 100% !important", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("gradient", styles, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AsNoTracking()", service, StringComparison.Ordinal);
        Assert.Contains("TripStatuses.Completed", service, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", service, StringComparison.Ordinal);
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

    private static string GetWebFile(params string[] segments) =>
        GetSolutionFile(["src", "OcpProduction.Web", .. segments]);

    private static string GetSolutionFile(params string[] segments)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(GetSourceFilePath())!);
        Assert.True(TryFindSolutionDirectory(ref directory));
        return Path.Combine(directory.FullName, Path.Combine(segments));
    }

    private static string GetSourceFilePath(
        [CallerFilePath] string sourceFilePath = "") =>
        sourceFilePath;

    private static bool TryFindSolutionDirectory(ref DirectoryInfo directory)
    {
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OcpProductionManagement.sln")))
        {
            directory = directory.Parent!;
        }

        return directory is not null;
    }
}
