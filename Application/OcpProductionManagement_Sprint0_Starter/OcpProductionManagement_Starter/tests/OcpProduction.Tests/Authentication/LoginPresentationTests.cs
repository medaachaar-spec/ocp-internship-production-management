using System.Runtime.CompilerServices;
using Xunit;

namespace OcpProduction.Tests.Authentication;

public sealed class LoginPresentationTests
{
    [Fact]
    public void Login_uses_the_approved_centered_proportional_Ocp_logo_without_changing_the_form()
    {
        var view = File.ReadAllText(GetWebFile("Views", "Account", "Login.cshtml"));
        var styles = File.ReadAllText(GetWebFile("wwwroot", "css", "site.css"));
        var logoPath = GetWebFile("wwwroot", "images", "branding", "OCP_logo.webp");

        Assert.True(File.Exists(logoPath));
        Assert.True(new FileInfo(logoPath).Length > 0);
        Assert.Contains("src=\"~/images/branding/OCP_logo.webp\"", view, StringComparison.Ordinal);
        Assert.Contains("alt=\"Logo OCP\"", view, StringComparison.Ordinal);
        Assert.Contains("class=\"login-brand-logo\"", view, StringComparison.Ordinal);
        Assert.Contains("class=\"login-brand-logo-wrap\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<div class=\"login-mark\">PM</div>", view, StringComparison.Ordinal);
        Assert.DoesNotContain(".login-mark", styles, StringComparison.Ordinal);

        Assert.Contains("Gestion de la production minière", view, StringComparison.Ordinal);
        Assert.Contains("OCP Phosboucraa", view, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Login\"", view, StringComparison.Ordinal);
        Assert.Contains("@Html.AntiForgeryToken()", view, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"ReturnUrl\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"Username\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"Password\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"RememberMe\"", view, StringComparison.Ordinal);
        Assert.Contains("Se connecter", view, StringComparison.Ordinal);
        Assert.Contains("Accès réservé aux utilisateurs autorisés.", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Prototype", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("démonstration", view, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(".login-brand-logo-wrap", styles, StringComparison.Ordinal);
        Assert.Contains("width: 60px; height: 60px", styles, StringComparison.Ordinal);
        Assert.Contains("margin: 0 auto 16px", styles, StringComparison.Ordinal);
        Assert.Contains("align-items: center; justify-content: center", styles, StringComparison.Ordinal);
        Assert.Contains(".login-brand-logo { display: block; width: 100%; height: 100%; object-fit: contain; }", styles, StringComparison.Ordinal);
        Assert.Contains(".login-card { width: min(430px, 100%)", styles, StringComparison.Ordinal);
        Assert.Contains(".login-page { min-height: 100vh; display: grid; place-items: center; padding: 24px", styles, StringComparison.Ordinal);
        Assert.Contains(".login-access-note", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(".demo-note", styles, StringComparison.Ordinal);
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
