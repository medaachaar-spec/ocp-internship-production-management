using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace OcpProduction.Tests.Presentation;

public sealed class FinalPresentationReadinessTests
{
    [Fact]
    public void User_visible_views_hide_unfinished_and_internal_terminology()
    {
        var viewsDirectory = Path.Combine(GetSolutionDirectory(), "src", "OcpProduction.Web", "Views");
        var forbidden = new Regex(
            @"\bPrototype\b|\bdémonstration\b|vw_[A-Za-z0-9_]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (var path in Directory.GetFiles(viewsDirectory, "*.cshtml", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            Assert.False(
                forbidden.IsMatch(source),
                $"Forbidden presentation terminology remains in {Path.GetRelativePath(viewsDirectory, path)}.");
        }
    }

    [Fact]
    public void Sidebar_has_no_unfinished_links_and_remains_keyboard_accessible_on_mobile()
    {
        var sidebar = ReadWebFile("Views", "Shared", "_Sidebar.cshtml");
        var styles = ReadWebFile("wwwroot", "css", "site.css");

        Assert.DoesNotContain(">Utilisateurs<", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain(">Paramètres<", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("nav-link disabled", sidebar, StringComparison.Ordinal);
        Assert.Contains("data-sidebar-toggle", sidebar, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"sidebar-navigation\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"false\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("navigation.classList.toggle(\"is-open\")", sidebar, StringComparison.Ordinal);
        Assert.Contains(".sidebar-menu-toggle:focus-visible", styles, StringComparison.Ordinal);
        Assert.Contains(".sidebar-nav.is-open { display: flex; }", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Reference_tables_have_accessible_names_and_headers_without_sql_implementation_wording()
    {
        foreach (var viewName in new[] { "WorkShifts.cshtml", "LoadingPoints.cshtml", "Destinations.cshtml", "StopReasons.cshtml" })
        {
            var source = ReadWebFile("Views", "ReferenceData", viewName);
            Assert.Contains("<caption class=\"visually-hidden\">", source, StringComparison.Ordinal);
            Assert.Contains("scope=\"col\"", source, StringComparison.Ordinal);
            Assert.Contains("scope=\"row\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SQL Server", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("contrainte SQL", source, StringComparison.OrdinalIgnoreCase);
        }

        var catalog = ReadWebFile("Views", "ReferenceData", "Index.cshtml");
        Assert.DoesNotContain("SQL Server", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("données de référence autorisées", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_routes_show_professional_safe_pages()
    {
        var program = ReadWebFile("Program.cs");
        var controller = ReadWebFile("Controllers", "HomeController.cs");
        var statusView = ReadWebFile("Views", "Home", "Status.cshtml");
        var accessDenied = ReadWebFile("Views", "Account", "AccessDenied.cshtml");

        Assert.Contains("UseStatusCodePagesWithReExecute", program, StringComparison.Ordinal);
        Assert.Contains("[AllowAnonymous]", controller, StringComparison.Ordinal);
        Assert.Contains("IActionResult Status(int code)", controller, StringComparison.Ordinal);
        Assert.Contains("Page introuvable", statusView, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", statusView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Votre rôle", accessDenied, StringComparison.Ordinal);
        Assert.DoesNotContain("Ton rôle", accessDenied, StringComparison.Ordinal);
    }

    private static string ReadWebFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(
            GetSolutionDirectory(),
            "src",
            "OcpProduction.Web",
            Path.Combine(segments)));

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
