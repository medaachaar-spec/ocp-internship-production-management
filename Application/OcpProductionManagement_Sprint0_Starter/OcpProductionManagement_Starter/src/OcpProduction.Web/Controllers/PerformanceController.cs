using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.Performance;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewReporting)]
[Route("Performance")]
public sealed class PerformanceController(
    IPerformanceReadService performanceReadService,
    ILogger<PerformanceController> logger) : Controller
{
    [HttpGet("Trucks")]
    public async Task<IActionResult> Trucks(
        [FromQuery] TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        try
        {
            var report = await performanceReadService.GetTruckReportAsync(
                normalized,
                cancellationToken);
            var filters = await performanceReadService.GetFilterOptionsAsync(
                cancellationToken);

            return View(new TruckPerformanceIndexViewModel
            {
                Criteria = normalized.WithPage(report.TableRows.Page),
                Report = report,
                FilterOptions = filters
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Truck performance reporting data could not be loaded.");

            return View(new TruckPerformanceIndexViewModel
            {
                Criteria = normalized,
                HasLoadError = true
            });
        }
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
        try
        {
            var report = await performanceReadService.GetDriverReportAsync(
                normalized,
                cancellationToken);
            var filters = await performanceReadService.GetFilterOptionsAsync(
                cancellationToken);

            return View(new DriverPerformanceIndexViewModel
            {
                Criteria = normalized.WithPage(report.TableRows.Page),
                Report = report,
                FilterOptions = filters
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Driver performance reporting data could not be loaded.");

            return View(new DriverPerformanceIndexViewModel
            {
                Criteria = normalized,
                HasLoadError = true
            });
        }
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
