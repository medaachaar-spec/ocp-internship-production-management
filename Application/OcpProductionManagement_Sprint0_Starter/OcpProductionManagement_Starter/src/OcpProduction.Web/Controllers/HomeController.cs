using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.ViewModels;

namespace OcpProduction.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ITripWorkflowService _tripWorkflowService;
    private readonly IStopWorkflowService _stopWorkflowService;

    public HomeController(
        IAuthorizationService authorizationService,
        ITripWorkflowService tripWorkflowService,
        IStopWorkflowService stopWorkflowService)
    {
        _authorizationService = authorizationService;
        _tripWorkflowService = tripWorkflowService;
        _stopWorkflowService = stopWorkflowService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var canValidate = await _authorizationService.AuthorizeAsync(
            User,
            PolicyNames.CanValidate);

        return View(new HomeDashboardViewModel
        {
            PendingValidationCount = canValidate.Succeeded
                ? await _tripWorkflowService.GetPendingValidationCountAsync(cancellationToken)
                : null,
            PendingStopValidationCount = canValidate.Succeeded
                ? await _stopWorkflowService.GetPendingValidationCountAsync(cancellationToken)
                : null
        });
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
