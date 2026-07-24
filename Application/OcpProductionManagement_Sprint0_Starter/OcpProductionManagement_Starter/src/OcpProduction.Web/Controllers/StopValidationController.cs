using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Web.ViewModels.Stops;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanValidate)]
[Route("Validation/Stops")]
public sealed class StopValidationController : Controller
{
    private readonly IStopWorkflowService _workflowService;
    private readonly IStopReadService _readService;

    public StopValidationController(
        IStopWorkflowService workflowService,
        IStopReadService readService)
    {
        _workflowService = workflowService;
        _readService = readService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] StopValidationSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await _workflowService.GetPendingValidationAsync(
            normalized,
            cancellationToken);
        var filterOptions = await _readService.GetFilterOptionsAsync(cancellationToken);

        return View(new PendingStopValidationIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpPost("{id:long}/Validate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var result = await _workflowService.ValidateAsync(id, cancellationToken);
        if (result.ErrorCode == StopWorkflowErrorCode.NotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ??
                result.ValidationErrors.Values.FirstOrDefault() ??
                "Cet arrêt a déjà été traité ou son état a changé.";
            return RedirectToAction(
                nameof(StopsController.Details),
                "Stops",
                new { id, fromValidation = true });
        }

        TempData["Success"] = "L’arrêt a été validé avec succès.";
        return RedirectToAction(nameof(Index));
    }
}
