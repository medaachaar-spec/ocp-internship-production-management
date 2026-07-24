using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopWorkflowControllerTests
{
    [Fact]
    public void Workflow_endpoints_use_documented_policies_routes_and_antiforgery()
    {
        var closeAuthorization = Assert.Single(
            typeof(StopWorkflowController).GetCustomAttributes<AuthorizeAttribute>());
        var validationAuthorization = Assert.Single(
            typeof(StopValidationController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(PolicyNames.CanManageStops, closeAuthorization.Policy);
        Assert.Equal(PolicyNames.CanValidate, validationAuthorization.Policy);
        Assert.Equal("Stops", Assert.Single(
            typeof(StopWorkflowController).GetCustomAttributes<RouteAttribute>()).Template);
        Assert.Equal("Validation/Stops", Assert.Single(
            typeof(StopValidationController).GetCustomAttributes<RouteAttribute>()).Template);

        var closePost = Assert.Single(
            typeof(StopWorkflowController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.GetCustomAttribute<HttpPostAttribute>() is not null);
        var validationPost = Assert.Single(
            typeof(StopValidationController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.GetCustomAttribute<HttpPostAttribute>() is not null);

        Assert.NotNull(closePost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(validationPost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal("Close/{id:long}", closePost.GetCustomAttribute<HttpPostAttribute>()!.Template);
        Assert.Equal("{id:long}/Validate", validationPost.GetCustomAttribute<HttpPostAttribute>()!.Template);
    }

    [Fact]
    public void No_forbidden_workflow_endpoint_exists()
    {
        var actions = new[] { typeof(StopWorkflowController), typeof(StopValidationController) }
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));

        Assert.DoesNotContain(
            actions,
            action => new[] { "Reject", "Reopen", "Cancel", "Delete", "Correct" }.Contains(action.Name));
    }

    [Fact]
    public async Task Get_Close_returns_confirmation_only_for_open_pending_stop()
    {
        var service = new FakeStopWorkflowService { ClosureData = CreateClosureData() };
        var controller = CreateCloseController(service);

        var result = await controller.Close(
            42,
            new StopSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var model = Assert.IsType<StopCloseViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(42, model.Input.StopId);
        Assert.Equal("TRK-001", model.ReturnCriteria.Search);

        service.ClosureData = CreateClosureData(
            endTime: new DateTime(2026, 7, 20, 9, 0, 0));
        var locked = await controller.Close(42, new StopSearchCriteria(), CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(locked);
    }

    [Fact]
    public async Task Post_Close_rejects_mismatch_invalid_input_and_maps_only_end_time()
    {
        var service = new FakeStopWorkflowService { ClosureData = CreateClosureData() };
        var mismatchController = CreateCloseController(service);
        var mismatch = await mismatchController.Close(
            41,
            CreateValidInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(mismatch);
        Assert.Equal(0, service.CloseCallCount);

        var invalidController = CreateCloseController(service);
        invalidController.ModelState.AddModelError("Input.EndTime", "L’heure de fin est obligatoire.");
        var invalid = await invalidController.Close(
            42,
            CreateValidInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(invalid);
        Assert.Equal(0, service.CloseCallCount);

        var validController = CreateCloseController(service);
        var valid = await validController.Close(
            42,
            CreateValidInput(),
            new StopSearchCriteria { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(valid);
        Assert.Equal(nameof(StopsController.Details), redirect.ActionName);
        Assert.Equal("Stops", redirect.ControllerName);
        Assert.Equal(new DateTime(2026, 7, 20, 9, 0, 0), service.LastCloseCommand?.EndTime);
        Assert.Equal("L’arrêt a été clôturé et transmis pour validation.", validController.TempData["Success"]);
    }

    [Fact]
    public async Task Pending_queue_normalizes_filters_and_validation_uses_only_route_id()
    {
        var service = new FakeStopWorkflowService
        {
            PendingResult = PagedResult<PendingStopListItemDto>.Create(
                [new PendingStopListItemDto
                {
                    StopId = 42,
                    TruckCode = "TRK-001",
                    StopReasonCode = "WAIT",
                    StopReasonName = "Attente"
                }],
                1,
                20,
                1)
        };
        var controller = CreateValidationController(service);

        var queue = await controller.Index(
            new StopValidationSearchCriteria { Search = "  TRK-001  ", PageSize = 999 },
            CancellationToken.None);

        var model = Assert.IsType<PendingStopValidationIndexViewModel>(
            Assert.IsType<ViewResult>(queue).Model);
        Assert.Equal("TRK-001", model.Criteria.Search);
        Assert.Equal(20, model.Criteria.PageSize);
        Assert.Equal(42, Assert.Single(model.Results.Items).StopId);

        var validation = await controller.Validate(42, CancellationToken.None);
        var redirect = Assert.IsType<RedirectToActionResult>(validation);
        Assert.Equal(nameof(StopValidationController.Index), redirect.ActionName);
        Assert.Equal(42, service.LastValidatedStopId);
        Assert.Equal("L’arrêt a été validé avec succès.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Invalid_validation_state_returns_to_safe_review()
    {
        var service = new FakeStopWorkflowService
        {
            ValidateResult = StopWorkflowResult.InvalidState(
                42,
                "Cet arrêt a déjà été traité ou son état a changé.")
        };
        var controller = CreateValidationController(service);

        var result = await controller.Validate(42, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StopsController.Details), redirect.ActionName);
        Assert.Equal("Stops", redirect.ControllerName);
        Assert.Equal("Cet arrêt a déjà été traité ou son état a changé.", controller.TempData["Error"]);
    }

    private static StopWorkflowController CreateCloseController(IStopWorkflowService service)
    {
        var httpContext = new DefaultHttpContext();
        return new StopWorkflowController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static StopValidationController CreateValidationController(
        IStopWorkflowService service)
    {
        var httpContext = new DefaultHttpContext();
        return new StopValidationController(service, new FakeStopReadService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static StopClosureData CreateClosureData(
        DateTime? endTime = null,
        string validationStatus = StopValidationStatuses.Pending) => new()
        {
            StopId = 42,
            TruckCode = "TRK-001",
            StopReasonCode = "WAIT",
            StopReasonName = "Attente",
            StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
            EndTime = endTime,
            ValidationStatus = validationStatus
        };

    private static StopCloseInputModel CreateValidInput() => new()
    {
        StopId = 42,
        EndTime = new DateTime(2026, 7, 20, 9, 0, 0)
    };

    private sealed class FakeStopWorkflowService : IStopWorkflowService
    {
        public StopClosureData? ClosureData { get; set; }

        public StopWorkflowResult CloseResult { get; init; } = StopWorkflowResult.Succeeded(42);

        public PagedResult<PendingStopListItemDto> PendingResult { get; init; } =
            PagedResult<PendingStopListItemDto>.Create([], 1, 20, 0);

        public StopWorkflowResult ValidateResult { get; init; } = StopWorkflowResult.Succeeded(42);

        public CloseStopCommand? LastCloseCommand { get; private set; }

        public long? LastValidatedStopId { get; private set; }

        public int CloseCallCount { get; private set; }

        public Task<StopClosureData?> GetForClosureAsync(
            long stopId,
            CancellationToken cancellationToken = default) => Task.FromResult(ClosureData);

        public Task<StopWorkflowResult> CloseAsync(
            CloseStopCommand command,
            CancellationToken cancellationToken = default)
        {
            CloseCallCount++;
            LastCloseCommand = command;
            return Task.FromResult(CloseResult);
        }

        public Task<PagedResult<PendingStopListItemDto>> GetPendingValidationAsync(
            StopValidationSearchCriteria criteria,
            CancellationToken cancellationToken = default) => Task.FromResult(PendingResult);

        public Task<long> GetPendingValidationCountAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(PendingResult.TotalItems);

        public Task<StopWorkflowResult> ValidateAsync(
            long stopId,
            CancellationToken cancellationToken = default)
        {
            LastValidatedStopId = stopId;
            return Task.FromResult(ValidateResult);
        }
    }

    private sealed class FakeStopReadService : IStopReadService
    {
        public Task<PagedResult<StopListItemDto>> SearchAsync(
            StopSearchCriteria criteria,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<StopListItemDto>.Create([], 1, 20, 0));

        public Task<StopDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) => Task.FromResult<StopDetailsDto?>(null);

        public Task<StopFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(new StopFilterOptions());
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }
}
