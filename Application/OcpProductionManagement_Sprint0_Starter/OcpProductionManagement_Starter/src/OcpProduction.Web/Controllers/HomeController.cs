using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.ViewModels;

namespace OcpProduction.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ITripReadService _tripReadService;
    private readonly IStopReadService _stopReadService;
    private readonly IQualityReadService _qualityReadService;
    private readonly ITripWorkflowService _tripWorkflowService;
    private readonly IStopWorkflowService _stopWorkflowService;

    public HomeController(
        IAuthorizationService authorizationService,
        ITripReadService tripReadService,
        IStopReadService stopReadService,
        IQualityReadService qualityReadService,
        ITripWorkflowService tripWorkflowService,
        IStopWorkflowService stopWorkflowService)
    {
        _authorizationService = authorizationService;
        _tripReadService = tripReadService;
        _stopReadService = stopReadService;
        _qualityReadService = qualityReadService;
        _tripWorkflowService = tripWorkflowService;
        _stopWorkflowService = stopWorkflowService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var canManageTrips = await HasPolicyAsync(PolicyNames.CanManageTrips);
        var canManageStops = await HasPolicyAsync(PolicyNames.CanManageStops);
        var canManageQuality = await HasPolicyAsync(PolicyNames.CanManageQuality);
        var canValidate = await HasPolicyAsync(PolicyNames.CanValidate);
        var canViewDashboard = await HasPolicyAsync(PolicyNames.CanViewDashboard);
        var canAdminister = await HasPolicyAsync(PolicyNames.CanAdminister);
        var canViewTechnicalDiagnostics = await HasPolicyAsync(
            PolicyNames.CanViewTechnicalDiagnostics);
        var today = DateOnly.FromDateTime(DateTime.Today);

        PagedResult<OcpProduction.Application.Trips.Dtos.TripListItemDto>? trips = null;
        PagedResult<OcpProduction.Application.Stops.Dtos.StopListItemDto>? openStops = null;
        PagedResult<OcpProduction.Application.Quality.Dtos.QualityResultListItemDto>? qualityAnalyses = null;

        if (canManageTrips)
        {
            trips = await _tripReadService.SearchAsync(
                new TripSearchCriteria
                {
                    DateFrom = today,
                    DateTo = today,
                    PageSize = 10
                },
                cancellationToken);
        }

        if (canManageStops)
        {
            openStops = await _stopReadService.SearchAsync(
                new StopSearchCriteria
                {
                    State = StopStates.Open,
                    PageSize = 10
                },
                cancellationToken);
        }

        if (canManageQuality)
        {
            qualityAnalyses = await _qualityReadService.SearchAsync(
                new QualitySearchCriteria
                {
                    DateFrom = today,
                    DateTo = today,
                    PageSize = 10
                },
                cancellationToken);
        }

        return View(new HomeDashboardViewModel
        {
            RoleDisplayName = GetRoleDisplayName(),
            CanManageTrips = canManageTrips,
            CanManageStops = canManageStops,
            CanManageQuality = canManageQuality,
            CanValidate = canValidate,
            CanViewDashboard = canViewDashboard,
            CanAdminister = canAdminister,
            CanViewTechnicalDiagnostics = canViewTechnicalDiagnostics,
            TripsTodayCount = trips?.TotalItems,
            OpenStopsCount = openStops?.TotalItems,
            QualityAnalysesTodayCount = qualityAnalyses?.TotalItems,
            PendingValidationCount = canValidate
                ? await _tripWorkflowService.GetPendingValidationCountAsync(cancellationToken)
                : null,
            PendingStopValidationCount = canValidate
                ? await _stopWorkflowService.GetPendingValidationCountAsync(cancellationToken)
                : null,
            RecentTrips = trips?.Items.Take(5).ToArray() ?? [],
            RecentOpenStops = openStops?.Items.Take(5).ToArray() ?? [],
            RecentQualityAnalyses = qualityAnalyses?.Items.Take(5).ToArray() ?? []
        });
    }

    private async Task<bool> HasPolicyAsync(string policyName)
    {
        var result = await _authorizationService.AuthorizeAsync(User, policyName);
        return result.Succeeded;
    }

    private string GetRoleDisplayName()
    {
        if (User.IsInRole(AppRoles.Administrateur))
        {
            return "administrateur";
        }

        if (User.IsInRole(AppRoles.ResponsableProduction))
        {
            return "responsable de production";
        }

        if (User.IsInRole(AppRoles.Superviseur))
        {
            return "superviseur";
        }

        if (User.IsInRole(AppRoles.AgentQualite))
        {
            return "agent qualité";
        }

        if (User.IsInRole(AppRoles.AgentProduction))
        {
            return "agent de production";
        }

        return "utilisateur";
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Status(int code)
    {
        var safeStatusCode = code is >= 400 and <= 599
            ? code
            : StatusCodes.Status404NotFound;
        Response.StatusCode = safeStatusCode;

        return View(safeStatusCode);
    }
}
