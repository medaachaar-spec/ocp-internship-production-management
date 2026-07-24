using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Web.ViewModels.Stops;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageStops)]
public sealed class StopsController : Controller
{
    private readonly IStopReadService _stopReadService;
    private readonly IStopWriteService _stopWriteService;
    private readonly IAuthorizationService _authorizationService;

    public StopsController(
        IStopReadService stopReadService,
        IStopWriteService stopWriteService,
        IAuthorizationService authorizationService)
    {
        _stopReadService = stopReadService;
        _stopWriteService = stopWriteService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] StopSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await _stopReadService.SearchAsync(normalized, cancellationToken);
        var filterOptions = await _stopReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new StopIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        long id,
        [FromQuery] StopSearchCriteria criteria,
        CancellationToken cancellationToken,
        [FromQuery] bool fromValidation = false)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var stop = await _stopReadService.GetByIdAsync(id, cancellationToken);
        if (stop is null)
        {
            return NotFound();
        }

        var tripAccess = await _authorizationService.AuthorizeAsync(
            User,
            resource: null,
            PolicyNames.CanManageTrips);
        var validationAccess = await _authorizationService.AuthorizeAsync(
            User,
            resource: null,
            PolicyNames.CanValidate);

        return View(new StopDetailsViewModel
        {
            Stop = stop,
            ReturnCriteria = criteria.Normalize(),
            CanViewRelatedTrip = stop.TripId.HasValue && tripAccess.Succeeded,
            CanValidate = validationAccess.Succeeded,
            FromValidation = fromValidation
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] StopSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModelAsync(
            new StopCreateInputModel { StartTime = DateTime.Now },
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Input")] StopCreateInputModel input,
        [Bind(Prefix = "ReturnCriteria")] StopSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateViewModelAsync(input, returnCriteria, cancellationToken));
        }

        var result = await _stopWriteService.CreateAsync(
            new CreateStopCommand
            {
                TripId = input.TripId,
                TruckId = input.TruckId.GetValueOrDefault(),
                DriverId = input.DriverId,
                WorkShiftId = input.WorkShiftId,
                StopReasonId = input.StopReasonId.GetValueOrDefault(),
                StartTime = input.StartTime.GetValueOrDefault(),
                Comments = input.Comments
            },
            cancellationToken);

        if (!result.Success)
        {
            AddWriteErrors(result);
            return View(await BuildCreateViewModelAsync(input, returnCriteria, cancellationToken));
        }

        TempData["Success"] = "L’arrêt a été créé avec succès.";
        return RedirectToAction(
            nameof(Details),
            StopNavigation.ForStop(returnCriteria.Normalize(), result.StopId!.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        long id,
        [FromQuery] StopSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var stop = await _stopWriteService.GetForEditAsync(id, cancellationToken);
        if (stop is null)
        {
            return NotFound();
        }

        if (!StopEditability.IsEditable(stop.EndTime, stop.ValidationStatus))
        {
            TempData["Error"] = "Cet arrêt n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                StopNavigation.ForStop(criteria.Normalize(), id));
        }

        return View(await BuildEditViewModelAsync(
            new StopEditInputModel
            {
                StopId = stop.StopId,
                TripId = stop.TripId,
                TruckId = stop.TruckId,
                DriverId = stop.DriverId,
                WorkShiftId = stop.WorkShiftId,
                StopReasonId = stop.StopReasonId,
                StartTime = stop.StartTime,
                Comments = stop.Comments
            },
            stop,
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind(Prefix = "Input")] StopEditInputModel input,
        [Bind(Prefix = "ReturnCriteria")] StopSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || input.StopId <= 0 || id != input.StopId)
        {
            return BadRequest();
        }

        var currentStop = await _stopWriteService.GetForEditAsync(id, cancellationToken);
        if (currentStop is null)
        {
            return NotFound();
        }

        if (!StopEditability.IsEditable(currentStop.EndTime, currentStop.ValidationStatus))
        {
            TempData["Error"] = "Cet arrêt n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                StopNavigation.ForStop(returnCriteria.Normalize(), id));
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEditViewModelAsync(
                input,
                currentStop,
                returnCriteria,
                cancellationToken));
        }

        var result = await _stopWriteService.UpdateAsync(
            new UpdateStopCommand
            {
                StopId = input.StopId,
                TripId = input.TripId,
                TruckId = input.TruckId.GetValueOrDefault(),
                DriverId = input.DriverId,
                WorkShiftId = input.WorkShiftId,
                StopReasonId = input.StopReasonId.GetValueOrDefault(),
                StartTime = input.StartTime.GetValueOrDefault(),
                Comments = input.Comments
            },
            cancellationToken);

        if (result.ErrorCode == StopWriteErrorCode.NotFound)
        {
            return NotFound();
        }

        if (result.ErrorCode == StopWriteErrorCode.InvalidState)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(
                nameof(Details),
                StopNavigation.ForStop(returnCriteria.Normalize(), id));
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

        TempData["Success"] = "L’arrêt a été modifié avec succès.";
        return RedirectToAction(
            nameof(Details),
            StopNavigation.ForStop(returnCriteria.Normalize(), input.StopId));
    }

    private async Task<StopFormViewModel<StopCreateInputModel>> BuildCreateViewModelAsync(
        StopCreateInputModel input,
        StopSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        return new StopFormViewModel<StopCreateInputModel>
        {
            Input = input,
            Options = await _stopWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = returnCriteria.Normalize(),
            PageTitle = "Nouvel arrêt",
            SubmitLabel = "Créer l’arrêt"
        };
    }

    private async Task<StopFormViewModel<StopEditInputModel>> BuildEditViewModelAsync(
        StopEditInputModel input,
        StopEditData? currentStop,
        StopSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        currentStop ??= await _stopWriteService.GetForEditAsync(input.StopId, cancellationToken);

        return new StopFormViewModel<StopEditInputModel>
        {
            Input = input,
            Options = await _stopWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = returnCriteria.Normalize(),
            PageTitle = $"Modifier l’arrêt #{input.StopId}",
            SubmitLabel = "Enregistrer les modifications",
            CurrentState = currentStop is null
                ? null
                : currentStop.EndTime is null ? StopStates.Open : StopStates.Closed,
            CurrentValidationStatus = currentStop?.ValidationStatus
        };
    }

    private void AddWriteErrors(StopWriteResult result)
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
