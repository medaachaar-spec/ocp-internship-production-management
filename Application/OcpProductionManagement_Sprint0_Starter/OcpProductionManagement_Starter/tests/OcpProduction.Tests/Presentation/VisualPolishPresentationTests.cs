using System.Runtime.CompilerServices;
using Xunit;

namespace OcpProduction.Tests.Presentation;

public sealed class VisualPolishPresentationTests
{
    [Fact]
    public void Detail_pages_use_shared_semantic_grouping_and_retain_factual_fields()
    {
        var details = new Dictionary<string, (string Source, string[] Fields)>
        {
            ["Trip"] = (
                ReadView("Trips", "Details.cshtml"),
                [
                    "Date opérationnelle", "Statut", "Camion", "Conducteur",
                    "Poste", "Point de chargement", "Destination", "Début",
                    "Fin", "Durée", "Tonnage", "Arrêts liés",
                    "Résultats qualité liés", "Dernier P2O5", "Observations"
                ]),
            ["Stop"] = (
                ReadView("Stops", "Details.cshtml"),
                [
                    "État opérationnel", "Validation", "Camion",
                    "Voyage associé", "Conducteur", "Poste", "Motif",
                    "Catégorie", "Planification", "Début", "Fin", "Durée",
                    "Contexte opérationnel", "Observation"
                ]),
            ["Truck"] = (
                ReadView("Trucks", "Details.cshtml"),
                [
                    "Code", "Immatriculation", "Statut enregistré",
                    "Référence active", "Enregistré le", "Modèle ou type",
                    "Capacité nominale", "Voyages", "Terminés",
                    "Tonnage total", "Première activité", "Dernière activité",
                    "Arrêts", "Durée totale", "Durée moyenne"
                ]),
            ["Driver"] = (
                ReadView("Drivers", "Details.cshtml"),
                [
                    "Conducteur", "Identifiant", "Statut enregistré",
                    "Référence active", "Enregistré le", "Voyages",
                    "Terminés", "Tonnage total", "Première activité",
                    "Dernière activité", "Arrêts associés",
                    "Durée totale liée", "Durée moyenne liée"
                ]),
            ["Quality"] = (
                ReadView("Quality", "Details.cshtml"),
                [
                    "Date d’analyse", "Créé le", "Statut enregistré",
                    "Mesure qualité", "Unité enregistrée",
                    "Valeur cible enregistrée", "Voyage associé", "Camion",
                    "Conducteur", "Poste", "Point de chargement",
                    "Destination", "Observation"
                ])
        };

        foreach (var (page, contract) in details)
        {
            Assert.Contains("<div class=\"detail-page\">", contract.Source, StringComparison.Ordinal);
            Assert.Contains("<header class=\"detail-header\">", contract.Source, StringComparison.Ordinal);
            Assert.Contains("detail-header__actions", contract.Source, StringComparison.Ordinal);
            Assert.Contains("detail-section-card", contract.Source, StringComparison.Ordinal);
            Assert.Contains("detail-definition-grid", contract.Source, StringComparison.Ordinal);
            Assert.Contains("<dl", contract.Source, StringComparison.Ordinal);
            Assert.Contains("<dt>", contract.Source, StringComparison.Ordinal);
            Assert.Contains("<dd>", contract.Source, StringComparison.Ordinal);
            Assert.Contains("aria-labelledby=", contract.Source, StringComparison.Ordinal);
            Assert.Contains("detail-footer-actions", contract.Source, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", contract.Source, StringComparison.Ordinal);

            foreach (var field in contract.Fields)
            {
                Assert.Contains(
                    field,
                    contract.Source,
                    StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Detail_actions_remain_conditionally_visible_without_duplicates()
    {
        var trip = ReadView("Trips", "Details.cshtml");
        var stop = ReadView("Stops", "Details.cshtml");
        var truck = ReadView("Trucks", "Details.cshtml");
        var driver = ReadView("Drivers", "Details.cshtml");
        var quality = ReadView("Quality", "Details.cshtml");

        Assert.Contains("@if (Model.CanEdit)", trip, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanClose)", trip, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanValidateCurrentTrip)", trip, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(trip, "asp-action=\"Edit\""));

        Assert.Contains("@if (Model.CanEdit)", stop, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanClose)", stop, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanValidateCurrentStop)", stop, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(stop, "asp-action=\"Edit\""));

        Assert.Contains("@if (Model.CanViewTrips)", truck, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewStops)", truck, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewTrips)", driver, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewStops)", driver, StringComparison.Ordinal);

        Assert.Contains("@if (Model.CanEdit)", quality, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewTrip)", quality, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(quality, "asp-action=\"Edit\""));
    }

    [Fact]
    public void Shared_visual_system_is_responsive_accessible_and_free_of_template_effects()
    {
        var siteCss = ReadWebFile("wwwroot", "css", "site.css");
        var performanceCss = ReadWebFile("wwwroot", "css", "performance.css");
        var chartTheme = ReadWebFile("wwwroot", "js", "chart-theme.js");
        var chartScripts = string.Join(
            Environment.NewLine,
            ReadWebFile("wwwroot", "js", "dashboard.js"),
            ReadWebFile("wwwroot", "js", "truck-performance.js"),
            ReadWebFile("wwwroot", "js", "driver-performance.js"));

        foreach (var token in new[]
                 {
                     "--ocp-green", "--ocp-green-dark", "--ocp-green-light",
                     "--ocp-blue-gray", "--ocp-muted-blue",
                     "--ocp-industrial-beige", "--text-strong",
                     "--text-muted", "--border-subtle", "--surface-muted",
                     "--radius-sm", "--radius-md", "--radius-lg",
                     "--shadow-subtle"
                 })
        {
            Assert.Contains(token, siteCss, StringComparison.Ordinal);
        }

        Assert.Contains(".detail-header", siteCss, StringComparison.Ordinal);
        Assert.Contains(".detail-definition-grid", siteCss, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 576px)", siteCss, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", performanceCss, StringComparison.Ordinal);
        Assert.Contains("width: 100% !important", siteCss, StringComparison.Ordinal);
        Assert.Contains("width: 100% !important", performanceCss, StringComparison.Ordinal);
        Assert.Contains("maintainAspectRatio: false", chartTheme, StringComparison.Ordinal);
        Assert.Contains("responsive: true", chartTheme, StringComparison.Ordinal);
        Assert.Contains("resizeDelay: 100", chartTheme, StringComparison.Ordinal);
        Assert.Contains("borderWidth: 1", chartTheme, StringComparison.Ordinal);
        Assert.Contains("barValueLabelsPlugin", chartTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("devicePixelRatio", chartTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("transform: scale", siteCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("transform: scale", performanceCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient", siteCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient", performanceCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chartjs-plugin", chartScripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ChartDataLabels", chartScripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ranking", chartScripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("best", chartScripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("worst", chartScripts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("red:", chartTheme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#dc3545", chartTheme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#ff0000", chartTheme, StringComparison.OrdinalIgnoreCase);
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

    private static string ReadView(params string[] segments) =>
        ReadWebFile(["Views", .. segments]);

    private static string ReadWebFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(GetSolutionDirectory(), "src", "OcpProduction.Web", Path.Combine(segments)));

    private static string GetSolutionDirectory(
        [CallerFilePath] string sourceFilePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath)!);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OcpProductionManagement.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}
