using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using OcpProduction.Application.Diagnostics;
using OcpProduction.Application.Security;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Diagnostics;
using Xunit;

namespace OcpProduction.Tests.Diagnostics;

public sealed class DiagnosticsRedesignTests
{
    [Fact]
    public void Controller_requires_the_dedicated_administrator_policy()
    {
        var authorize = Assert.Single(
            typeof(DiagnosticsController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(PolicyNames.CanViewTechnicalDiagnostics, authorize.Policy);
    }

    [Fact]
    public async Task Failed_check_returns_a_restrained_safe_model()
    {
        const string safeMessage =
            "SQL Server ou base de données inaccessible. Consultez les journaux techniques.";
        var controller = new DiagnosticsController(
            new FakeDatabaseHealthService(new DatabaseHealthResult(
                false,
                safeMessage,
                "localhost",
                "SensitiveDatabase")),
            new FakeWebHostEnvironment { EnvironmentName = "Development" });

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DiagnosticsViewModel>(view.Model);
        Assert.True(model.ApplicationAvailable);
        Assert.False(model.DatabaseConnected);
        Assert.Equal(safeMessage, model.DatabaseMessage);
        Assert.Equal("Development", model.EnvironmentName);
        Assert.False(string.IsNullOrWhiteSpace(model.ApplicationVersion));
        Assert.NotEqual(default, model.LastCheckedAt);
        Assert.Null(typeof(DiagnosticsViewModel).GetProperty("Server"));
        Assert.Null(typeof(DiagnosticsViewModel).GetProperty("Database"));
        Assert.Null(typeof(DiagnosticsViewModel).GetProperty("CurrentUser"));
        Assert.Null(typeof(DiagnosticsViewModel).GetProperty("CurrentRole"));
    }

    [Fact]
    public void View_and_sidebar_hide_sensitive_details_and_gate_diagnostics_by_policy()
    {
        var view = File.ReadAllText(GetWebFile("Views", "Diagnostics", "Index.cshtml"));
        var sidebar = File.ReadAllText(GetWebFile("Views", "Shared", "_Sidebar.cshtml"));

        Assert.Contains("État des services nécessaires au fonctionnement de l’application", view, StringComparison.Ordinal);
        Assert.Contains("Actualiser le diagnostic", view, StringComparison.Ordinal);
        Assert.Contains("Dernière vérification", view, StringComparison.Ordinal);
        Assert.Contains("Version de l’application", view, StringComparison.Ordinal);
        Assert.Contains("@if (canViewTechnicalDiagnostics)", sidebar, StringComparison.Ordinal);
        Assert.Contains("PolicyNames.CanViewTechnicalDiagnostics", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.Server", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.Database ??", view, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentUser", view, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentRole", view, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chaîne de connexion :", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user id", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sprint 0", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Database_health_check_is_bounded_read_only_and_uses_safe_messages()
    {
        var source = File.ReadAllText(GetSolutionFile(
            "src",
            "OcpProduction.Infrastructure",
            "Data",
            "SqlServerHealthService.cs"));

        Assert.Contains("ConnectTimeout = 5", source, StringComparison.Ordinal);
        Assert.Contains("OpenAsync(cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("SQL Server accessible. Base de données accessible.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteNonQuery", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
    }

    private static string GetWebFile(params string[] segments) =>
        GetSolutionFile(["src", "OcpProduction.Web", .. segments]);

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

    private sealed class FakeDatabaseHealthService(
        DatabaseHealthResult result) : IDatabaseHealthService
    {
        public Task<DatabaseHealthResult> CheckAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "OcpProduction.Web";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
