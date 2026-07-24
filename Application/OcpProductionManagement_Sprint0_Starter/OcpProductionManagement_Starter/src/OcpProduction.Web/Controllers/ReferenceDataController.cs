using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.ReferenceData.Models;
using OcpProduction.Application.ReferenceData.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.ViewModels.ReferenceData;

namespace OcpProduction.Web.Controllers;

[Authorize(Policy = PolicyNames.CanAdminister)]
[Route("ReferenceData")]
public sealed class ReferenceDataController(
    IReferenceDataReadService readService,
    IReferenceDataWriteService writeService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new ReferenceCatalogViewModel
        {
            Items = await readService.GetCatalogAsync(cancellationToken)
        });
    }

    [HttpGet("WorkShifts")]
    public async Task<IActionResult> WorkShifts(CancellationToken cancellationToken)
    {
        return View(new WorkShiftListViewModel { Items = await readService.GetWorkShiftsAsync(cancellationToken) });
    }

    [HttpGet("WorkShifts/Create")]
    public IActionResult CreateWorkShift()
    {
        return View("WorkShiftForm", new WorkShiftFormViewModel());
    }

    [HttpPost("WorkShifts/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWorkShift(WorkShiftInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || input.StartTime is null || input.EndTime is null)
        {
            return View("WorkShiftForm", new WorkShiftFormViewModel { Input = input });
        }

        var result = await writeService.CreateWorkShiftAsync(
            new CreateWorkShiftCommand(input.Code, input.Name, input.StartTime.Value, input.EndTime.Value, input.IsActive),
            cancellationToken);
        return HandleWriteResult(result, nameof(WorkShifts), () => View("WorkShiftForm", new WorkShiftFormViewModel { Input = input }));
    }

    [HttpGet("WorkShifts/{id:int}/Edit")]
    public async Task<IActionResult> EditWorkShift(int id, CancellationToken cancellationToken)
    {
        var item = await readService.GetWorkShiftAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.CanEdit)
        {
            TempData["Error"] = "Ce poste est déjà utilisé et ne peut pas être modifié.";
            return RedirectToAction(nameof(WorkShifts));
        }

        return View("WorkShiftForm", new WorkShiftFormViewModel
        {
            Id = item.Id,
            ImmutableCode = item.Code,
            IsEdit = true,
            Input = new WorkShiftInputModel { Code = item.Code, Name = item.Name, StartTime = item.StartTime, EndTime = item.EndTime, IsActive = item.IsActive }
        });
    }

    [HttpPost("WorkShifts/{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWorkShift(int id, WorkShiftInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || input.StartTime is null || input.EndTime is null)
        {
            return View("WorkShiftForm", new WorkShiftFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input });
        }

        var result = await writeService.UpdateWorkShiftAsync(
            new UpdateWorkShiftCommand(id, input.Name, input.StartTime.Value, input.EndTime.Value, input.IsActive),
            cancellationToken);
        return HandleWriteResult(result, nameof(WorkShifts), () => View("WorkShiftForm", new WorkShiftFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input }));
    }

    [HttpPost("WorkShifts/{id:int}/Active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetWorkShiftActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        return HandleWriteResult(
            await writeService.SetWorkShiftActiveAsync(id, isActive, cancellationToken),
            nameof(WorkShifts),
            () => RedirectToAction(nameof(WorkShifts)));
    }

    [HttpGet("LoadingPoints")]
    public async Task<IActionResult> LoadingPoints(CancellationToken cancellationToken)
    {
        return View(new LoadingPointListViewModel { Items = await readService.GetLoadingPointsAsync(cancellationToken) });
    }

    [HttpGet("LoadingPoints/Create")]
    public IActionResult CreateLoadingPoint()
    {
        return View("LoadingPointForm", new LoadingPointFormViewModel());
    }

    [HttpPost("LoadingPoints/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLoadingPoint(LoadingPointInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("LoadingPointForm", new LoadingPointFormViewModel { Input = input });
        }

        var result = await writeService.CreateLoadingPointAsync(
            new CreateLoadingPointCommand(input.Code, input.Name, input.ZoneName, input.Description, input.IsActive),
            cancellationToken);
        return HandleWriteResult(result, nameof(LoadingPoints), () => View("LoadingPointForm", new LoadingPointFormViewModel { Input = input }));
    }

    [HttpGet("LoadingPoints/{id:int}/Edit")]
    public async Task<IActionResult> EditLoadingPoint(int id, CancellationToken cancellationToken)
    {
        var item = await readService.GetLoadingPointAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.CanEdit)
        {
            TempData["Error"] = "Ce point de chargement est déjà utilisé et ne peut pas être modifié.";
            return RedirectToAction(nameof(LoadingPoints));
        }

        return View("LoadingPointForm", new LoadingPointFormViewModel
        {
            Id = item.Id,
            ImmutableCode = item.Code,
            IsEdit = true,
            Input = new LoadingPointInputModel { Code = item.Code, Name = item.Name, ZoneName = item.ZoneName, Description = item.Description, IsActive = item.IsActive }
        });
    }

    [HttpPost("LoadingPoints/{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLoadingPoint(int id, LoadingPointInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("LoadingPointForm", new LoadingPointFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input });
        }

        var result = await writeService.UpdateLoadingPointAsync(
            new UpdateLoadingPointCommand(id, input.Name, input.ZoneName, input.Description, input.IsActive),
            cancellationToken);
        return HandleWriteResult(result, nameof(LoadingPoints), () => View("LoadingPointForm", new LoadingPointFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input }));
    }

    [HttpPost("LoadingPoints/{id:int}/Active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLoadingPointActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        return HandleWriteResult(await writeService.SetLoadingPointActiveAsync(id, isActive, cancellationToken), nameof(LoadingPoints), () => RedirectToAction(nameof(LoadingPoints)));
    }

    [HttpGet("Destinations")]
    public async Task<IActionResult> Destinations(CancellationToken cancellationToken)
    {
        return View(new DestinationListViewModel { Items = await readService.GetDestinationsAsync(cancellationToken) });
    }

    [HttpGet("Destinations/Create")]
    public IActionResult CreateDestination()
    {
        return View("DestinationForm", new DestinationFormViewModel());
    }

    [HttpPost("Destinations/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDestination(DestinationInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("DestinationForm", new DestinationFormViewModel { Input = input });
        }

        var result = await writeService.CreateDestinationAsync(new CreateDestinationCommand(input.Code, input.Name, input.Type, input.IsActive), cancellationToken);
        return HandleWriteResult(result, nameof(Destinations), () => View("DestinationForm", new DestinationFormViewModel { Input = input }));
    }

    [HttpGet("Destinations/{id:int}/Edit")]
    public async Task<IActionResult> EditDestination(int id, CancellationToken cancellationToken)
    {
        var item = await readService.GetDestinationAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.CanEdit)
        {
            TempData["Error"] = "Cette destination est déjà utilisée et ne peut pas être modifiée.";
            return RedirectToAction(nameof(Destinations));
        }

        return View("DestinationForm", new DestinationFormViewModel
        {
            Id = item.Id,
            ImmutableCode = item.Code,
            IsEdit = true,
            Input = new DestinationInputModel { Code = item.Code, Name = item.Name, Type = item.Type, IsActive = item.IsActive }
        });
    }

    [HttpPost("Destinations/{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDestination(int id, DestinationInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("DestinationForm", new DestinationFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input });
        }

        var result = await writeService.UpdateDestinationAsync(new UpdateDestinationCommand(id, input.Name, input.Type, input.IsActive), cancellationToken);
        return HandleWriteResult(result, nameof(Destinations), () => View("DestinationForm", new DestinationFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input }));
    }

    [HttpPost("Destinations/{id:int}/Active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDestinationActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        return HandleWriteResult(await writeService.SetDestinationActiveAsync(id, isActive, cancellationToken), nameof(Destinations), () => RedirectToAction(nameof(Destinations)));
    }

    [HttpGet("StopReasons")]
    public async Task<IActionResult> StopReasons(CancellationToken cancellationToken)
    {
        return View(new StopReasonListViewModel { Items = await readService.GetStopReasonsAsync(cancellationToken) });
    }

    [HttpGet("StopReasons/Create")]
    public IActionResult CreateStopReason()
    {
        return View("StopReasonForm", new StopReasonFormViewModel());
    }

    [HttpPost("StopReasons/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStopReason(StopReasonInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("StopReasonForm", new StopReasonFormViewModel { Input = input });
        }

        var result = await writeService.CreateStopReasonAsync(new CreateStopReasonCommand(input.Code, input.Name, input.Category, input.IsPlanned, input.IsActive), cancellationToken);
        return HandleWriteResult(result, nameof(StopReasons), () => View("StopReasonForm", new StopReasonFormViewModel { Input = input }));
    }

    [HttpGet("StopReasons/{id:int}/Edit")]
    public async Task<IActionResult> EditStopReason(int id, CancellationToken cancellationToken)
    {
        var item = await readService.GetStopReasonAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.CanEdit)
        {
            TempData["Error"] = "Ce motif d'arrêt est déjà utilisé et ne peut pas être modifié.";
            return RedirectToAction(nameof(StopReasons));
        }

        return View("StopReasonForm", new StopReasonFormViewModel
        {
            Id = item.Id,
            ImmutableCode = item.Code,
            IsEdit = true,
            Input = new StopReasonInputModel { Code = item.Code, Name = item.Name, Category = item.Category, IsPlanned = item.IsPlanned, IsActive = item.IsActive }
        });
    }

    [HttpPost("StopReasons/{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStopReason(int id, StopReasonInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("StopReasonForm", new StopReasonFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input });
        }

        var result = await writeService.UpdateStopReasonAsync(new UpdateStopReasonCommand(id, input.Name, input.Category, input.IsPlanned, input.IsActive), cancellationToken);
        return HandleWriteResult(result, nameof(StopReasons), () => View("StopReasonForm", new StopReasonFormViewModel { Id = id, ImmutableCode = input.Code, IsEdit = true, Input = input }));
    }

    [HttpPost("StopReasons/{id:int}/Active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStopReasonActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        return HandleWriteResult(await writeService.SetStopReasonActiveAsync(id, isActive, cancellationToken), nameof(StopReasons), () => RedirectToAction(nameof(StopReasons)));
    }

    private IActionResult HandleWriteResult(ReferenceDataWriteResult result, string successAction, Func<IActionResult> failureView)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(successAction);
        }

        TempData["Error"] = result.Message;
        return failureView();
    }
}
