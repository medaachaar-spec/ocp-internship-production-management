using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Quality.Commands;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.Quality;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageQuality)]
public sealed class QualityController : Controller
{
    private readonly IQualityReadService _qualityReadService;
    private readonly IQualityWriteService _qualityWriteService;
    private readonly IAuthorizationService _authorizationService;

    public QualityController(
        IQualityReadService qualityReadService,
        IQualityWriteService qualityWriteService,
        IAuthorizationService authorizationService)
    {
        _qualityReadService = qualityReadService;
        _qualityWriteService = qualityWriteService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var normalized = criteria.Normalize();
        var results = await _qualityReadService.SearchAsync(normalized, cancellationToken);
        var filterOptions = await _qualityReadService.GetFilterOptionsAsync(cancellationToken);

        return View(new QualityIndexViewModel
        {
            Criteria = normalized.WithPage(results.Page),
            Results = results,
            FilterOptions = filterOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        long id,
        [FromQuery] QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id < 0)
        {
            return NotFound();
        }

        var result = await _qualityReadService.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        var canViewTrip = result.TripId.HasValue &&
            (await _authorizationService.AuthorizeAsync(
                User,
                PolicyNames.CanManageTrips)).Succeeded;

        return View(new QualityDetailsViewModel
        {
            Result = result,
            ReturnCriteria = criteria.Normalize(),
            CanViewTrip = canViewTrip
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModelAsync(
            new QualityCreateInputModel { AnalysisDate = DateTime.Now },
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Input")] QualityCreateInputModel input,
        [Bind(Prefix = "ReturnCriteria")] QualitySearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateViewModelAsync(
                input,
                returnCriteria,
                cancellationToken));
        }

        var result = await _qualityWriteService.CreateAsync(
            new CreateQualityResultCommand
            {
                AnalysisDate = input.AnalysisDate.GetValueOrDefault(),
                TripId = input.TripId,
                LoadingPointId = input.LoadingPointId,
                QualityValue = input.QualityValue.GetValueOrDefault(),
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

        TempData["Success"] = "Le résultat qualité a été enregistré avec succès.";
        return RedirectToAction(
            nameof(Details),
            QualityNavigation.ForResult(
                returnCriteria.Normalize(),
                result.QualityResultId!.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        long id,
        [FromQuery] QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        if (id < 0)
        {
            return NotFound();
        }

        var result = await _qualityWriteService.GetForEditAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        if (!QualityEditability.IsEditable(result.Status))
        {
            TempData["Error"] = "Ce résultat qualité n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                QualityNavigation.ForResult(criteria.Normalize(), id));
        }

        return View(await BuildEditViewModelAsync(
            new QualityEditInputModel
            {
                QualityResultId = result.QualityResultId,
                AnalysisDate = result.AnalysisDate,
                TripId = result.TripId,
                LoadingPointId = result.LoadingPointId,
                QualityValue = result.QualityValue,
                Comments = result.Comments
            },
            result.Status,
            criteria,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind(Prefix = "Input")] QualityEditInputModel input,
        [Bind(Prefix = "ReturnCriteria")] QualitySearchCriteria returnCriteria,
        CancellationToken cancellationToken)
    {
        if (id < 0 || input.QualityResultId < 0 || id != input.QualityResultId)
        {
            return BadRequest();
        }

        var currentResult = await _qualityWriteService.GetForEditAsync(id, cancellationToken);
        if (currentResult is null)
        {
            return NotFound();
        }

        if (!QualityEditability.IsEditable(currentResult.Status))
        {
            TempData["Error"] = "Ce résultat qualité n’est plus modifiable dans son état actuel.";
            return RedirectToAction(
                nameof(Details),
                QualityNavigation.ForResult(returnCriteria.Normalize(), id));
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEditViewModelAsync(
                input,
                currentResult.Status,
                returnCriteria,
                cancellationToken));
        }

        var result = await _qualityWriteService.UpdateAsync(
            new UpdateQualityResultCommand
            {
                QualityResultId = input.QualityResultId,
                AnalysisDate = input.AnalysisDate.GetValueOrDefault(),
                TripId = input.TripId,
                LoadingPointId = input.LoadingPointId,
                QualityValue = input.QualityValue.GetValueOrDefault(),
                Comments = input.Comments
            },
            cancellationToken);

        if (!result.Success)
        {
            AddWriteErrors(result);
            return View(await BuildEditViewModelAsync(
                input,
                currentResult.Status,
                returnCriteria,
                cancellationToken));
        }

        TempData["Success"] = "Le résultat qualité a été modifié avec succès.";
        return RedirectToAction(
            nameof(Details),
            QualityNavigation.ForResult(returnCriteria.Normalize(), id));
    }

    private async Task<QualityFormViewModel<QualityCreateInputModel>> BuildCreateViewModelAsync(
        QualityCreateInputModel input,
        QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return new QualityFormViewModel<QualityCreateInputModel>
        {
            Input = input,
            Options = await _qualityWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = criteria.Normalize(),
            PageTitle = "Nouvelle analyse qualité",
            SubmitLabel = "Enregistrer"
        };
    }

    private async Task<QualityFormViewModel<QualityEditInputModel>> BuildEditViewModelAsync(
        QualityEditInputModel input,
        string currentStatus,
        QualitySearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return new QualityFormViewModel<QualityEditInputModel>
        {
            Input = input,
            Options = await _qualityWriteService.GetFormOptionsAsync(cancellationToken),
            ReturnCriteria = criteria.Normalize(),
            PageTitle = $"Modifier l’analyse #{input.QualityResultId}",
            SubmitLabel = "Enregistrer les modifications",
            CurrentStatus = currentStatus
        };
    }

    private void AddWriteErrors(QualityWriteResult result)
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
