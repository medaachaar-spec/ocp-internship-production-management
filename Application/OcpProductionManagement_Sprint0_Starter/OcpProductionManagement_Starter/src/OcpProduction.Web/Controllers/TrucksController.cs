using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Application.Trucks.Services;
using OcpProduction.Web.ViewModels.Trucks;

namespace OcpProduction.Web.Controllers;

[Authorize(Roles = FleetAccessRoles.All)]
public sealed class TrucksController(
    ITruckReadService truckReadService,
    IAuthorizationService authorizationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] TruckSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await truckReadService.SearchAsync(normalized, cancellationToken);
        var filterOptions = await truckReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new TruckIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        [FromQuery] TruckSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var truck = await truckReadService.GetByIdAsync(id, cancellationToken);
        if (truck is null)
        {
            return NotFound();
        }

        var tripAccess = await authorizationService.AuthorizeAsync(User, PolicyNames.CanManageTrips);
        var stopAccess = await authorizationService.AuthorizeAsync(User, PolicyNames.CanManageStops);

        return View(new TruckDetailsViewModel
        {
            Truck = truck,
            ReturnCriteria = criteria.Normalize(),
            CanViewTrips = tripAccess.Succeeded,
            CanViewStops = stopAccess.Succeeded
        });
    }
}
