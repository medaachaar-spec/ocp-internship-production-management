using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Drivers.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.Drivers;

namespace OcpProduction.Web.Controllers;

[Authorize(Roles = FleetAccessRoles.All)]
public sealed class DriversController(
    IDriverReadService driverReadService,
    IAuthorizationService authorizationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] DriverSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await driverReadService.SearchAsync(normalized, cancellationToken);
        var filterOptions = await driverReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new DriverIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        [FromQuery] DriverSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var driver = await driverReadService.GetByIdAsync(id, cancellationToken);
        if (driver is null)
        {
            return NotFound();
        }

        var tripAccess = await authorizationService.AuthorizeAsync(User, PolicyNames.CanManageTrips);
        var stopAccess = await authorizationService.AuthorizeAsync(User, PolicyNames.CanManageStops);

        return View(new DriverDetailsViewModel
        {
            Driver = driver,
            ReturnCriteria = criteria.Normalize(),
            CanViewTrips = tripAccess.Succeeded,
            CanViewStops = stopAccess.Succeeded
        });
    }
}
