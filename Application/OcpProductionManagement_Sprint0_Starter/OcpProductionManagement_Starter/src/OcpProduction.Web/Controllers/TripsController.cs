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
public sealed class TripsController : Controller
{
    private readonly ITripReadService _tripReadService;
    private readonly ITripWriteService _tripWriteService;

    public TripsController(
        ITripReadService tripReadService,
        ITripWriteService tripWriteService)
    {
        _tripReadService = tripReadService;
        _tripWriteService = tripWriteService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] TripSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await _tripReadService.SearchAsync(normalized, cancellationToken);
        var filterOptions = await _tripReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new TripIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        long id,
        [FromQuery] TripSearchCriteria criteria,
        CancellationToken cancellationToken,
        [FromQuery] bool fromValidation = false)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var trip = await _tripReadService.GetByIdAsync(id, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        return View(new TripDetailsViewModel
        {
            Trip = trip,
            ReturnCriteria = criteria.Normalize(),
            CanValidate = User.IsInRole(AppRoles.Superviseur),
            FromValidation = fromValidation
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] TripSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModelAsync(
            new TripCreateInputModel(),
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Input")] TripCreateInputModel input,
        [Bind(Prefix = "ReturnCriteria")] TripSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateViewModelAsync(
                input,
                returnCriteria,
                cancellationToken));
        }

        var result = await _tripWriteService.CreateAsync(
            new CreateTripCommand
            {
                TripDate = input.TripDate.GetValueOrDefault(),
                WorkShiftId = input.WorkShiftId.GetValueOrDefault(),
                TruckId = input.TruckId.GetValueOrDefault(),
                DriverId = input.DriverId.GetValueOrDefault(),
                LoadingPointId = input.LoadingPointId.GetValueOrDefault(),
                DestinationId = input.DestinationId.GetValueOrDefault(),
                StartTime = input.StartTime.GetValueOrDefault(),
                Comments = input.Comments
            },
            cancellationToken);

        if (!result.Success)
        {
            AddWriteErrors(result);
            return View(await BuildCreateViewModelAsync(
                input,
                returnCriteria,
                cancellationToken));
        }

        TempData["Success"] = "Le voyage a été créé avec succès.";
        return RedirectToAction(
            nameof(Details),
            TripNavigation.ForTrip(
                returnCriteria.Normalize(),
                result.TripId!.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        long id,
        [FromQuery] TripSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var trip = await _tripWriteService.GetForEditAsync(id, cancellationToken);
        if (trip is null)
        {
            return NotFound();
        }

        if (!TripStatuses.IsEditable(trip.Status))
        {
            TempData["Error"] = "Ce voyage n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                TripNavigation.ForTrip(criteria.Normalize(), id));
        }

        return View(await BuildEditViewModelAsync(
            new TripEditInputModel
            {
                TripId = trip.TripId,
                TripDate = trip.TripDate,
                WorkShiftId = trip.WorkShiftId,
                TruckId = trip.TruckId,
                DriverId = trip.DriverId,
                LoadingPointId = trip.LoadingPointId,
                DestinationId = trip.DestinationId,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                Tonnage = trip.Tonnage,
                Comments = trip.Comments
            },
            trip.Status,
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind(Prefix = "Input")] TripEditInputModel input,
        [Bind(Prefix = "ReturnCriteria")] TripSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || input.TripId <= 0 || id != input.TripId)
        {
            return BadRequest();
        }

        var currentTrip = await _tripWriteService.GetForEditAsync(id, cancellationToken);
        if (currentTrip is null)
        {
            return NotFound();
        }

        if (!TripStatuses.IsEditable(currentTrip.Status))
        {
            TempData["Error"] = "Ce voyage n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                TripNavigation.ForTrip(returnCriteria.Normalize(), id));
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEditViewModelAsync(
                input,
                currentTrip.Status,
                returnCriteria,
                cancellationToken));
        }

        var result = await _tripWriteService.UpdateAsync(
            new UpdateTripCommand
            {
                TripId = input.TripId,
                TripDate = input.TripDate.GetValueOrDefault(),
                WorkShiftId = input.WorkShiftId.GetValueOrDefault(),
                TruckId = input.TruckId.GetValueOrDefault(),
                DriverId = input.DriverId.GetValueOrDefault(),
                LoadingPointId = input.LoadingPointId.GetValueOrDefault(),
                DestinationId = input.DestinationId.GetValueOrDefault(),
                StartTime = input.StartTime.GetValueOrDefault(),
                EndTime = input.EndTime,
                Tonnage = input.Tonnage,
                Comments = input.Comments
            },
            cancellationToken);

        if (result.ErrorCode == TripWriteErrorCode.NotFound)
        {
            return NotFound();
        }

        if (result.ErrorCode == TripWriteErrorCode.InvalidState)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(
                nameof(Details),
                TripNavigation.ForTrip(returnCriteria.Normalize(), id));
        }

        if (!result.Success)
        {
            AddWriteErrors(result);
            return View(await BuildEditViewModelAsync(
                input,
                null,
                returnCriteria,
                cancellationToken));
        }

        TempData["Success"] = "Le voyage a été modifié avec succès.";
        return RedirectToAction(
            nameof(Details),
            TripNavigation.ForTrip(returnCriteria.Normalize(), input.TripId));
    }

    private async Task<TripFormViewModel<TripCreateInputModel>> BuildCreateViewModelAsync(
        TripCreateInputModel input,
        TripSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        return new TripFormViewModel<TripCreateInputModel>
        {
            Input = input,
            Options = await _tripWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = returnCriteria.Normalize()
        };
    }

    private async Task<TripFormViewModel<TripEditInputModel>> BuildEditViewModelAsync(
        TripEditInputModel input,
        string? currentStatus,
        TripSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        currentStatus ??= (await _tripWriteService.GetForEditAsync(
            input.TripId,
            cancellationToken))?.Status;

        return new TripFormViewModel<TripEditInputModel>
        {
            Input = input,
            Options = await _tripWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = returnCriteria.Normalize(),
            CurrentStatus = currentStatus
        };
    }

    private void AddWriteErrors(TripWriteResult result)
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
