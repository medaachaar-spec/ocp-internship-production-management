using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Diagnostics;
using OcpProduction.Web.ViewModels.Diagnostics;

namespace OcpProduction.Web.Controllers;

[Authorize]
public sealed class DiagnosticsController : Controller
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public DiagnosticsController(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.CheckAsync(cancellationToken);
        var model = new DiagnosticsViewModel
        {
            DatabaseConnected = result.IsConnected,
            DatabaseMessage = result.Message,
            Server = result.Server,
            Database = result.Database,
            EnvironmentName = HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            CurrentUser = User.Identity?.Name ?? "Inconnu",
            CurrentRole = User.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "Aucun"
        };

        return View(model);
    }
}
