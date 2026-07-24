using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.ViewModels.Trips;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanValidate)]
[Route("Validation/Trips")]
public sealed class ValidationController : Controller
{
    private readonly ITripWorkflowService _workflowService;

    public ValidationController(ITripWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] TripValidationSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await _workflowService.GetPendingValidationAsync(
            normalized,
            cancellationToken);

        return View(new PendingValidationIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results
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
        if (result.ErrorCode == TripWorkflowErrorCode.NotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ??
                result.ValidationErrors.Values.FirstOrDefault() ??
                "Ce voyage a déjà été traité ou son état a changé.";
            return RedirectToAction(
                nameof(TripsController.Details),
                "Trips",
                new { id, fromValidation = true });
        }

        TempData["Success"] = "Le voyage a été validé avec succès.";
        return RedirectToAction(nameof(Index));
    }
}
