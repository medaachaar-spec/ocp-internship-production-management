using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Diagnostics;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.Diagnostics;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewTechnicalDiagnostics)]
public sealed class DiagnosticsController : Controller
{
    private readonly IDatabaseHealthService _databaseHealthService;
    private readonly IWebHostEnvironment _environment;

    public DiagnosticsController(
        IDatabaseHealthService databaseHealthService,
        IWebHostEnvironment environment)
    {
        _databaseHealthService = databaseHealthService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.CheckAsync(cancellationToken);
        var model = new DiagnosticsViewModel
        {
            ApplicationAvailable = true,
            DatabaseConnected = result.IsConnected,
            DatabaseMessage = result.Message,
            EnvironmentName = _environment.EnvironmentName,
            ApplicationVersion = GetApplicationVersion(),
            LastCheckedAt = DateTimeOffset.Now
        };

        return View(model);
    }

    private static string GetApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
            ?? "Non disponible";
    }
}
