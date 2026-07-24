using System.Reflection;
using System.Security.Claims;
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

public sealed class StopsControllerTests
{
    [Fact]
    public void Controller_uses_CanManageStops_and_exposes_only_authorized_part2_actions()
    {
        var authorize = Assert.Single(
            typeof(StopsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PolicyNames.CanManageStops, authorize.Policy);

        var actions = typeof(StopsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Equal(
            [nameof(StopsController.Create), nameof(StopsController.Create), nameof(StopsController.Details), nameof(StopsController.Edit), nameof(StopsController.Edit), nameof(StopsController.Index)],
            actions.Select(action => action.Name).OrderBy(name => name));

        var posts = actions
            .Where(action => action.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();
        Assert.Equal(2, posts.Length);
        Assert.All(posts, action =>
            Assert.NotNull(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
        Assert.DoesNotContain(actions, action =>
            new[] { "Close", "Validate", "Reject", "Reopen", "Delete" }.Contains(action.Name));
    }

    [Fact]
    public async Task Index_returns_view_with_normalized_criteria_and_options()
    {
        var service = new FakeStopReadService
        {
            SearchResult = PagedResult<StopListItemDto>.Create(
                [new StopListItemDto
                {
                    StopId = 42,
                    TruckCode = "TRK-007",
                    StopReasonCode = "WAIT",
                    StopReasonName = "Attente",
                    State = StopStates.Closed,
                    ValidationStatus = StopValidationStatuses.Validated
                }],
                1,
                20,
                1),
            FilterOptions = new StopFilterOptions
            {
                Trucks = [new StopFilterOption(7, "TRK-007")]
            }
        };
        var controller = CreateController(service);

        var result = await controller.Index(
            new StopSearchCriteria { Search = "  TRK-007  ", Page = -1, PageSize = 999 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StopIndexViewModel>(view.Model);
        Assert.Equal("TRK-007", service.LastSearchCriteria?.Search);
        Assert.Equal(1, service.LastSearchCriteria?.Page);
        Assert.Equal(20, service.LastSearchCriteria?.PageSize);
        Assert.Equal(42, Assert.Single(model.Results.Items).StopId);
        Assert.Equal("42", model.DetailsRouteValues(42)["id"]);
    }

    [Fact]
    public async Task Details_returns_not_found_for_invalid_or_missing_identifier()
    {
        var service = new FakeStopReadService();
        var controller = CreateController(service);

        var invalid = await controller.Details(
            0,
            new StopSearchCriteria(),
            CancellationToken.None);
        var missing = await controller.Details(
            9999,
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(invalid);
        Assert.IsType<NotFoundResult>(missing);
        Assert.Equal(1, service.GetByIdCallCount);
    }

    [Fact]
    public async Task Details_returns_view_and_preserves_navigation_when_stop_exists()
    {
        var service = new FakeStopReadService
        {
            Details = new StopDetailsDto
            {
                StopId = 42,
                TripId = 73,
                TruckCode = "TRK-007",
                StopReasonCode = "WAIT",
                StopReasonName = "Attente",
                State = StopStates.Closed,
                ValidationStatus = StopValidationStatuses.Validated
            }
        };
        var controller = CreateController(service, canViewTrips: true);

        var result = await controller.Details(
            42,
            new StopSearchCriteria { Search = "  TRK-007  ", Page = 2, PageSize = 10 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StopDetailsViewModel>(view.Model);
        Assert.Equal(42, model.Stop.StopId);
        Assert.True(model.CanViewRelatedTrip);
        Assert.Equal("TRK-007", model.ReturnCriteria.Search);
        Assert.Equal("2", model.ReturnRouteValues[nameof(StopSearchCriteria.Page)]);
    }

    [Fact]
    public async Task Details_hides_related_trip_link_without_trip_policy()
    {
        var service = new FakeStopReadService
        {
            Details = new StopDetailsDto
            {
                StopId = 42,
                TripId = 73,
                TruckCode = "TRK-007",
                StopReasonCode = "WAIT",
                StopReasonName = "Attente",
                State = StopStates.Closed,
                ValidationStatus = StopValidationStatuses.Validated
            }
        };
        var controller = CreateController(service, canViewTrips: false);

        var result = await controller.Details(
            42,
            new StopSearchCriteria(),
            CancellationToken.None);

        var model = Assert.IsType<StopDetailsViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.False(model.CanViewRelatedTrip);
    }

    [Fact]
    public async Task Get_Create_returns_form_with_options_and_navigation()
    {
        var writeService = new FakeStopWriteService
        {
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Create(
            new StopSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var model = Assert.IsType<StopFormViewModel<StopCreateInputModel>>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("TRK-001", model.ReturnCriteria.Search);
        Assert.Equal("TRK-001", Assert.Single(model.Options.Trucks).Label);
        Assert.NotNull(model.Input.StartTime);
    }

    [Fact]
    public async Task Invalid_Create_returns_form_without_calling_write_service()
    {
        var writeService = new FakeStopWriteService { FormOptions = CreateFormOptions() };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);
        controller.ModelState.AddModelError("Input.TruckId", "Le camion est obligatoire.");

        var result = await controller.Create(
            new StopCreateInputModel(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, writeService.CreateCallCount);
    }

    [Fact]
    public async Task Valid_Create_redirects_to_Details_and_maps_only_editable_fields()
    {
        var writeService = new FakeStopWriteService
        {
            CreateResult = StopWriteResult.Succeeded(901)
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Create(
            CreateValidCreateInput(),
            new StopSearchCriteria { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StopsController.Details), redirect.ActionName);
        Assert.Equal("901", redirect.RouteValues!["id"]);
        Assert.Equal("2", redirect.RouteValues[nameof(StopSearchCriteria.Page)]);
        Assert.Equal(1, writeService.LastCreateCommand?.TruckId);
        Assert.Equal("L’arrêt a été créé avec succès.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_service_validation_error_returns_form_and_field_error()
    {
        var writeService = new FakeStopWriteService
        {
            CreateResult = StopWriteResult.ValidationFailed(
                new Dictionary<string, string>
                {
                    [nameof(CreateStopCommand.TruckId)] = "Le camion doit correspondre au voyage indiqué."
                })
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Create(
            CreateValidCreateInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Single(controller.ModelState["Input.TruckId"]!.Errors);
    }

    [Fact]
    public async Task Get_Edit_returns_not_found_for_missing_stop()
    {
        var controller = CreateController(new FakeStopReadService());

        var result = await controller.Edit(9999, new StopSearchCriteria(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_Edit_refuses_non_editable_stop()
    {
        var writeService = new FakeStopWriteService
        {
            EditData = CreateEditData(endTime: new DateTime(2026, 7, 20, 9, 0, 0))
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Edit(42, new StopSearchCriteria(), CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StopsController.Details), redirect.ActionName);
        Assert.Equal("Cet arrêt n’est plus modifiable dans son état actuel.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Get_Edit_returns_form_for_open_pending_stop()
    {
        var writeService = new FakeStopWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Edit(42, new StopSearchCriteria(), CancellationToken.None);

        var model = Assert.IsType<StopFormViewModel<StopEditInputModel>>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(42, model.Input.StopId);
        Assert.Equal(StopStates.Open, model.CurrentState);
        Assert.Equal(StopValidationStatuses.Pending, model.CurrentValidationStatus);
    }

    [Fact]
    public async Task Post_Edit_rejects_route_and_form_identifier_mismatch()
    {
        var writeService = new FakeStopWriteService { EditData = CreateEditData() };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Edit(
            41,
            CreateValidEditInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Forged_Post_Edit_cannot_bypass_edit_lock()
    {
        var writeService = new FakeStopWriteService
        {
            EditData = CreateEditData(validationStatus: StopValidationStatuses.Validated)
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Invalid_Edit_returns_form_without_writing()
    {
        var writeService = new FakeStopWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);
        controller.ModelState.AddModelError("Input.StartTime", "Date invalide.");

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Valid_Edit_redirects_and_invalid_state_is_handled_safely()
    {
        var successfulService = new FakeStopWriteService
        {
            EditData = CreateEditData(),
            UpdateResult = StopWriteResult.Succeeded(42)
        };
        var successfulController = CreateController(
            new FakeStopReadService(),
            writeService: successfulService);

        var successful = await successfulController.Edit(
            42,
            CreateValidEditInput(),
            new StopSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var successfulRedirect = Assert.IsType<RedirectToActionResult>(successful);
        Assert.Equal(nameof(StopsController.Details), successfulRedirect.ActionName);
        Assert.Equal("TRK-001", successfulRedirect.RouteValues![nameof(StopSearchCriteria.Search)]);
        Assert.Equal(42, successfulService.LastUpdateCommand?.StopId);

        var lockedService = new FakeStopWriteService
        {
            EditData = CreateEditData(),
            UpdateResult = StopWriteResult.InvalidState(42, "Cet arrêt n’est plus modifiable.")
        };
        var lockedController = CreateController(new FakeStopReadService(), writeService: lockedService);

        var locked = await lockedController.Edit(
            42,
            CreateValidEditInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(locked);
        Assert.Equal("Cet arrêt n’est plus modifiable.", lockedController.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_concurrency_conflict_returns_form_with_safe_error()
    {
        var writeService = new FakeStopWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions(),
            UpdateResult = StopWriteResult.ConcurrencyConflict(
                42,
                "L’arrêt a été modifié ou supprimé pendant votre saisie.")
        };
        var controller = CreateController(new FakeStopReadService(), writeService: writeService);

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new StopSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Single(controller.ModelState[string.Empty]!.Errors);
    }

    private static StopCreateInputModel CreateValidCreateInput() => new()
    {
        TripId = 73,
        TruckId = 1,
        DriverId = 2,
        WorkShiftId = 3,
        StopReasonId = 4,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        Comments = "Observation"
    };

    private static StopEditInputModel CreateValidEditInput() => new()
    {
        StopId = 42,
        TripId = 73,
        TruckId = 1,
        DriverId = 2,
        WorkShiftId = 3,
        StopReasonId = 4,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        Comments = "Observation"
    };

    private static StopEditData CreateEditData(
        DateTime? endTime = null,
        string validationStatus = StopValidationStatuses.Pending) => new()
        {
            StopId = 42,
            TripId = 73,
            TruckId = 1,
            DriverId = 2,
            WorkShiftId = 3,
            StopReasonId = 4,
            StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
            EndTime = endTime,
            ValidationStatus = validationStatus,
            Comments = "Observation"
        };

    private static StopFormOptions CreateFormOptions() => new()
    {
        Trucks = [new StopFormOption(1, "TRK-001")],
        Drivers = [new StopFormOption(2, "DRV-002")],
        WorkShifts = [new StopFormOption(3, "SHIFT-3")],
        StopReasons = [new StopFormOption(4, "WAIT - Attente")]
    };

    private static StopsController CreateController(
        IStopReadService service,
        bool canViewTrips = false,
        IStopWriteService? writeService = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var controller = new StopsController(
            service,
            writeService ?? new FakeStopWriteService(),
            new FakeAuthorizationService(canViewTrips));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    private sealed class FakeStopReadService : IStopReadService
    {
        public PagedResult<StopListItemDto> SearchResult { get; init; } =
            PagedResult<StopListItemDto>.Create([], 1, 20, 0);

        public StopDetailsDto? Details { get; init; }

        public StopFilterOptions FilterOptions { get; init; } = new();

        public StopSearchCriteria? LastSearchCriteria { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public Task<PagedResult<StopListItemDto>> SearchAsync(
            StopSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastSearchCriteria = criteria;
            return Task.FromResult(SearchResult);
        }

        public Task<StopDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(Details);
        }

        public Task<StopFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FilterOptions);
    }

    private sealed class FakeStopWriteService : IStopWriteService
    {
        public StopWriteResult CreateResult { get; init; } = StopWriteResult.Succeeded(42);

        public StopWriteResult UpdateResult { get; init; } = StopWriteResult.Succeeded(42);

        public StopEditData? EditData { get; init; }

        public StopFormOptions FormOptions { get; init; } = new();

        public CreateStopCommand? LastCreateCommand { get; private set; }

        public UpdateStopCommand? LastUpdateCommand { get; private set; }

        public int CreateCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public Task<StopWriteResult> CreateAsync(
            CreateStopCommand command,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            LastCreateCommand = command;
            return Task.FromResult(CreateResult);
        }

        public Task<StopEditData?> GetForEditAsync(
            long id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EditData);

        public Task<StopWriteResult> UpdateAsync(
            UpdateStopCommand command,
            CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            LastUpdateCommand = command;
            return Task.FromResult(UpdateResult);
        }

        public Task<StopFormOptions> GetFormOptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FormOptions);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class FakeAuthorizationService(bool succeeds) : IAuthorizationService
    {
        private readonly AuthorizationResult _result = succeeds
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed();

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(_result);

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName) =>
            Task.FromResult(_result);
    }
}
