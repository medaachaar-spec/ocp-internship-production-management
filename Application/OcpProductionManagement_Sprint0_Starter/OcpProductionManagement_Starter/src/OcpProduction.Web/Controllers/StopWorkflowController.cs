using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Web.ViewModels.Stops;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageStops)]
[Route("Stops")]
public sealed class StopWorkflowController : Controller
{
    private readonly IStopWorkflowService _workflowService;

    public StopWorkflowController(IStopWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("Close/{id:long}")]
    public async Task<IActionResult> Close(
        long id,
        [FromQuery] StopSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var stop = await _workflowService.GetForClosureAsync(id, cancellationToken);
        if (stop is null)
        {
            return NotFound();
        }

        if (!StopWorkflowRules.CanClose(stop.EndTime, stop.ValidationStatus))
        {
            TempData["Error"] = "Cet arrêt ne peut plus être clôturé dans son état actuel.";
            return RedirectToAction(
                nameof(StopsController.Details),
                "Stops",
                StopNavigation.ForStop(criteria.Normalize(), id));
        }

        return View(
            "~/Views/Stops/Close.cshtml",
            BuildViewModel(stop, criteria, new StopCloseInputModel
            {
                StopId = stop.StopId
            }));
    }

    [HttpPost("Close/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(
        long id,
        [Bind(Prefix = "Input")] StopCloseInputModel input,
        [Bind(Prefix = "ReturnCriteria")] StopSearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || input.StopId <= 0 || id != input.StopId)
        {
            return BadRequest();
        }

        var stop = await _workflowService.GetForClosureAsync(id, cancellationToken);
        if (stop is null)
        {
            return NotFound();
        }

        if (!StopWorkflowRules.CanClose(stop.EndTime, stop.ValidationStatus))
        {
            TempData["Error"] = "Cet arrêt ne peut plus être clôturé dans son état actuel.";
            return RedirectToAction(
                nameof(StopsController.Details),
                "Stops",
                StopNavigation.ForStop(returnCriteria.Normalize(), id));
        }

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Stops/Close.cshtml",
                BuildViewModel(stop, returnCriteria, input));
        }

        var result = await _workflowService.CloseAsync(
            new CloseStopCommand
            {
                StopId = id,
                EndTime = input.EndTime!.Value
            },
            cancellationToken);

        if (result.ErrorCode == StopWorkflowErrorCode.NotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            AddWorkflowErrors(result);
            return View(
                "~/Views/Stops/Close.cshtml",
                BuildViewModel(stop, returnCriteria, input));
        }

        TempData["Success"] = "L’arrêt a été clôturé et transmis pour validation.";
        return RedirectToAction(
            nameof(StopsController.Details),
            "Stops",
            StopNavigation.ForStop(returnCriteria.Normalize(), id));
    }

    private static StopCloseViewModel BuildViewModel(
        StopClosureData stop,
        StopSearchCriteria criteria,
        StopCloseInputModel input) => new()
        {
            Stop = stop,
            Input = input,
            ReturnCriteria = criteria.Normalize()
        };

    private void AddWorkflowErrors(StopWorkflowResult result)
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
