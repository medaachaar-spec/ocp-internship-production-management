using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.Performance;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewReporting)]
[Route("Performance")]
public sealed class PerformanceController(IPerformanceReadService performanceReadService) : Controller
{
    [HttpGet("Trucks")]
    public async Task<IActionResult> Trucks(
        [FromQuery] TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await performanceReadService.SearchTrucksAsync(normalized, cancellationToken);
        var filters = await performanceReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new TruckPerformanceIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filters
        });
    }

    [HttpGet("Trucks/{id:int}")]
    public async Task<IActionResult> TruckDetails(
        int id,
        [FromQuery] TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var performance = await performanceReadService.GetTruckAsync(id, cancellationToken);
        if (performance is null)
        {
            return NotFound();
        }

        return View(new TruckPerformanceDetailsViewModel
        {
            Performance = performance,
            ReturnCriteria = criteria.Normalize()
        });
    }

    [HttpGet("Drivers")]
    public async Task<IActionResult> Drivers(
        [FromQuery] DriverPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await performanceReadService.SearchDriversAsync(normalized, cancellationToken);
        var filters = await performanceReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new DriverPerformanceIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filters
        });
    }

    [HttpGet("Drivers/{id:int}")]
    public async Task<IActionResult> DriverDetails(
        int id,
        [FromQuery] DriverPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var performance = await performanceReadService.GetDriverAsync(id, cancellationToken);
        if (performance is null)
        {
            return NotFound();
        }

        return View(new DriverPerformanceDetailsViewModel
        {
            Performance = performance,
            ReturnCriteria = criteria.Normalize()
        });
    }
}
