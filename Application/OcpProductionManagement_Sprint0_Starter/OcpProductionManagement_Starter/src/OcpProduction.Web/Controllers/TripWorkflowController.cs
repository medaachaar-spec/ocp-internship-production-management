using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.ViewModels.Trips;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageTrips)]
[Route("Trips")]
public sealed class TripWorkflowController : Controller
{
    private readonly ITripWorkflowService _workflowService;

    public TripWorkflowController(ITripWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("Close/{id:long}")]
    public async Task<IActionResult> Close(
        long id,
        [FromQuery] TripSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var trip = await _workflowService.GetForClosureAsync(id, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        if (!TripStatuses.CanBeClosed(trip.Status))
        {
            TempData["Error"] = "Ce voyage ne peut plus être clôturé dans son état actuel.";
            return RedirectToAction(
                nameof(TripsController.Details),
                "Trips",
                TripNavigation.ForTrip(criteria.Normalize(), id));
        }

        return View(
            "~/Views/Trips/Close.cshtml",
            BuildViewModel(trip, criteria, new TripCloseInputModel
            {
                TripId = trip.TripId,
                EndTime = trip.EndTime,
                Tonnage = trip.Tonnage
            }));
    }

    [HttpPost("Close/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(
        long id,
        [Bind(Prefix = "Input")] TripCloseInputModel input,
        [Bind(Prefix = "ReturnCriteria")] TripSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || input.TripId <= 0 || id != input.TripId)
        {
            return BadRequest();
        }

        var trip = await _workflowService.GetForClosureAsync(id, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        if (!TripStatuses.CanBeClosed(trip.Status))
        {
            TempData["Error"] = "Ce voyage ne peut plus être clôturé dans son état actuel.";
            return RedirectToAction(
                nameof(TripsController.Details),
                "Trips",
                TripNavigation.ForTrip(returnCriteria.Normalize(), id));
        }

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Trips/Close.cshtml",
                BuildViewModel(trip, returnCriteria, input));
        }

        var result = await _workflowService.CloseAsync(
            new CloseTripCommand
            {
                TripId = id,
                EndTime = input.EndTime!.Value,
                Tonnage = input.Tonnage!.Value
            },
            cancellationToken);

        if (result.ErrorCode == TripWorkflowErrorCode.NotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            AddWorkflowErrors(result);
            return View(
                "~/Views/Trips/Close.cshtml",
                BuildViewModel(trip, returnCriteria, input));
        }

        TempData["Success"] = "Le voyage a été clôturé et transmis pour validation.";
        return RedirectToAction(
            nameof(TripsController.Details),
            "Trips",
            TripNavigation.ForTrip(returnCriteria.Normalize(), id));
    }

    private static TripCloseViewModel BuildViewModel(
        TripClosureData trip,
        TripSearchCriteria criteria,
        TripCloseInputModel input) => new()
        {
            Trip = trip,
            Input = input,
            ReturnCriteria = criteria.Normalize()
        };

    private void AddWorkflowErrors(TripWorkflowResult result)
    {
        foreach (var error in result.ValidationErrors)
        {
            ModelState.AddModelError($"Input.{error.Key}", error.Value);
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }
    }
}
