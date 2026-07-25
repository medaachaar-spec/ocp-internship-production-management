using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Application.Dashboard.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ModelBinding;
using OcpProduction.Web.ViewModels.Dashboard;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewDashboard)]
[Route("Dashboard")]
public sealed class DashboardController(
    IDashboardReadService dashboardReadService,
    ILogger<DashboardController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery, ModelBinder(BinderType = typeof(DashboardDateCriteriaModelBinder))]
        DashboardDateCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (criteria.HasInvalidRange)
        {
            ModelState.AddModelError(
                string.Empty,
                "La date de début doit être antérieure ou égale à la date de fin.");
        }

        if (!ModelState.IsValid)
        {
            return View(CreateViewModel(criteria, new DashboardDto()));
        }

        try
        {
            var dashboard = await dashboardReadService.GetDashboardAsync(
                criteria,
                cancellationToken);

            return View(CreateViewModel(criteria, dashboard));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Dashboard reporting data could not be loaded.");

            return View(CreateViewModel(
                criteria,
                new DashboardDto(),
                hasLoadError: true));
        }
    }

    private static DashboardViewModel CreateViewModel(
        DashboardDateCriteria criteria,
        DashboardDto dashboard,
        bool hasLoadError = false)
    {
        return new DashboardViewModel
        {
            Criteria = criteria,
            Kpis = dashboard,
            HasLoadError = hasLoadError
        };
    }
}
