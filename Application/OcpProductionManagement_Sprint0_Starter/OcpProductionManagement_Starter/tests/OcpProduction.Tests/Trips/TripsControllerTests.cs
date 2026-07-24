using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripsControllerTests
{
    [Fact]
    public void Controller_requires_the_existing_CanManageTrips_policy()
    {
        var attribute = Assert.Single(
            typeof(TripsController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(PolicyNames.CanManageTrips, attribute.Policy);
    }

    [Fact]
    public void Write_actions_are_antiforgery_protected_and_forbidden_actions_do_not_exist()
    {
        var actions = typeof(TripsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var postActions = actions
            .Where(action => action.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();

        Assert.Equal(2, postActions.Length);
        Assert.Contains(postActions, action => action.Name == nameof(TripsController.Create));
        Assert.Contains(postActions, action => action.Name == nameof(TripsController.Edit));
        Assert.All(
            postActions,
            action => Assert.NotNull(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));

        var forbiddenActions = new[] { "Delete", "Close", "Submit", "Validate", "Reject", "Cancel" };
        Assert.DoesNotContain(actions, action => forbiddenActions.Contains(action.Name));
    }

    [Fact]
    public async Task Index_returns_view_with_normalized_criteria_and_filter_options()
    {
        var readService = new FakeTripReadService
        {
            SearchResult = PagedResult<TripListItemDto>.Create(
                [new TripListItemDto { TripId = 42 }],
                page: 1,
                pageSize: 20,
                totalItems: 1),
            FilterOptions = new TripFilterOptions
            {
                Trucks = [new TripFilterOption(7, "TRK-007")]
            }
        };
        var controller = CreateController(readService, new FakeTripWriteService());

        var result = await controller.Index(
            new TripSearchCriteria
            {
                Search = "  TRK-007  ",
                Page = 0,
                PageSize = 999
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TripIndexViewModel>(view.Model);
        Assert.Equal("TRK-007", readService.LastSearchCriteria?.Search);
        Assert.Equal(1, readService.LastSearchCriteria?.Page);
        Assert.Equal(20, readService.LastSearchCriteria?.PageSize);
        Assert.Equal(42, Assert.Single(model.Results.Items).TripId);
        Assert.Equal("TRK-007", Assert.Single(model.FilterOptions.Trucks).Label);
        Assert.Equal("42", model.DetailsRouteValues(42)["id"]);
        Assert.Equal("42", model.EditRouteValues(42)["id"]);
        Assert.Equal("TRK-007", model.CreateRouteValues[nameof(TripSearchCriteria.Search)]);
    }

    [Fact]
    public async Task Details_returns_not_found_for_invalid_or_missing_identifier()
    {
        var readService = new FakeTripReadService();
        var controller = CreateController(readService, new FakeTripWriteService());

        var invalidResult = await controller.Details(
            0,
            new TripSearchCriteria(),
            CancellationToken.None);
        var missingResult = await controller.Details(
            9999,
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(invalidResult);
        Assert.IsType<NotFoundResult>(missingResult);
        Assert.Equal(1, readService.GetByIdCallCount);
    }

    [Fact]
    public async Task Details_returns_view_for_existing_trip_and_preserves_normalized_return_filters()
    {
        var readService = new FakeTripReadService
        {
            TripDetails = new TripDetailsDto { TripId = 42 }
        };
        var controller = CreateController(readService, new FakeTripWriteService());

        var result = await controller.Details(
            42,
            new TripSearchCriteria
            {
                Search = "  TRK-007  ",
                Page = 2,
                PageSize = 10
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TripDetailsViewModel>(view.Model);
        Assert.Equal(42, model.Trip.TripId);
        Assert.Equal("TRK-007", model.ReturnCriteria.Search);
        Assert.Equal("2", model.ReturnRouteValues[nameof(TripSearchCriteria.Page)]);
        Assert.Equal("10", model.ReturnRouteValues[nameof(TripSearchCriteria.PageSize)]);
        Assert.Equal("42", model.EditRouteValues["id"]);
    }

    [Fact]
    public async Task Get_Create_returns_form_with_real_service_options()
    {
        var writeService = new FakeTripWriteService
        {
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Create(
            new TripSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TripFormViewModel<TripCreateInputModel>>(view.Model);
        Assert.Equal("TRK-001", model.ReturnCriteria.Search);
        Assert.Equal("TRK-001", Assert.Single(model.Options.Trucks).Label);
    }

    [Fact]
    public async Task Invalid_Create_returns_form_without_calling_write_operation()
    {
        var writeService = new FakeTripWriteService { FormOptions = CreateFormOptions() };
        var controller = CreateController(new FakeTripReadService(), writeService);
        controller.ModelState.AddModelError("Input.TruckId", "Le camion est obligatoire.");

        var result = await controller.Create(
            new TripCreateInputModel(),
            new TripSearchCriteria(),
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<TripFormViewModel<TripCreateInputModel>>(view.Model);
        Assert.Equal(0, writeService.CreateCallCount);
        Assert.Equal(1, writeService.FormOptionsCallCount);
    }

    [Fact]
    public async Task Valid_Create_redirects_to_Details_and_sets_safe_success_message()
    {
        var writeService = new FakeTripWriteService
        {
            CreateResult = TripWriteResult.Succeeded(901)
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Create(
            CreateValidCreateInput(),
            new TripSearchCriteria { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal("901", redirect.RouteValues!["id"]);
        Assert.Equal("2", redirect.RouteValues[nameof(TripSearchCriteria.Page)]);
        Assert.Equal("Le voyage a été créé avec succès.", controller.TempData["Success"]);
        Assert.Equal(901, writeService.LastCreateCommand is null ? null : writeService.CreateResult.TripId);
    }

    [Fact]
    public async Task Create_service_validation_error_returns_form_and_field_error()
    {
        var writeService = new FakeTripWriteService
        {
            CreateResult = TripWriteResult.ValidationFailed(
                new Dictionary<string, string>
                {
                    [nameof(CreateTripCommand.TruckId)] =
                        "Le camion sélectionné n’existe pas ou n’est pas disponible."
                })
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Create(
            CreateValidCreateInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        var error = Assert.Single(controller.ModelState["Input.TruckId"]!.Errors);
        Assert.Contains("n’existe pas", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_Edit_returns_not_found_for_missing_trip()
    {
        var controller = CreateController(
            new FakeTripReadService(),
            new FakeTripWriteService());

        var result = await controller.Edit(
            9999,
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_Edit_returns_form_for_existing_trip()
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            42,
            new TripSearchCriteria { PageSize = 10 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TripFormViewModel<TripEditInputModel>>(view.Model);
        Assert.Equal(42, model.Input.TripId);
        Assert.Equal("InProgress", model.CurrentStatus);
        Assert.Equal(10, model.ReturnCriteria.PageSize);
    }

    [Theory]
    [InlineData("PendingValidation")]
    [InlineData("Completed")]
    public async Task Get_Edit_redirects_non_editable_status_to_read_only_details(string status)
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(status)
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            42,
            new TripSearchCriteria(),
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal("Ce voyage n’est plus modifiable dans son état actuel.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Post_Edit_rejects_mismatched_route_and_input_identifiers()
    {
        var writeService = new FakeTripWriteService();
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            41,
            CreateValidEditInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Invalid_Edit_returns_form_without_calling_write_operation()
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeTripReadService(), writeService);
        controller.ModelState.AddModelError("Input.EndTime", "Date invalide.");

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, writeService.UpdateCallCount);
        Assert.Equal(1, writeService.FormOptionsCallCount);
    }

    [Fact]
    public async Task Valid_Edit_redirects_to_Details_and_sets_safe_success_message()
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(),
            UpdateResult = TripWriteResult.Succeeded(42)
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new TripSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal("42", redirect.RouteValues!["id"]);
        Assert.Equal("TRK-001", redirect.RouteValues[nameof(TripSearchCriteria.Search)]);
        Assert.Equal("Le voyage a été modifié avec succès.", controller.TempData["Success"]);
        Assert.Equal(42, writeService.LastUpdateCommand?.TripId);
    }

    [Theory]
    [InlineData("PendingValidation")]
    [InlineData("Completed")]
    public async Task Forged_Post_Edit_cannot_bypass_status_lock(string status)
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(status)
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Edit_concurrency_conflict_returns_form_with_safe_error()
    {
        var writeService = new FakeTripWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions(),
            UpdateResult = TripWriteResult.ConcurrencyConflict(
                42,
                "Le voyage a été modifié ou supprimé pendant votre saisie.")
        };
        var controller = CreateController(new FakeTripReadService(), writeService);

        var result = await controller.Edit(
            42,
            CreateValidEditInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Contains("modifié ou supprimé", error.ErrorMessage, StringComparison.Ordinal);
    }

    private static TripsController CreateController(
        ITripReadService readService,
        ITripWriteService writeService)
    {
        var httpContext = new DefaultHttpContext();
        return new TripsController(readService, writeService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static TripCreateInputModel CreateValidCreateInput() => new()
    {
        TripDate = new DateOnly(2026, 7, 20),
        WorkShiftId = 1,
        TruckId = 1,
        DriverId = 1,
        LoadingPointId = 1,
        DestinationId = 1,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        Comments = "Création de test"
    };

    private static TripEditInputModel CreateValidEditInput() => new()
    {
        TripId = 42,
        TripDate = new DateOnly(2026, 7, 20),
        WorkShiftId = 1,
        TruckId = 1,
        DriverId = 1,
        LoadingPointId = 1,
        DestinationId = 1,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        EndTime = new DateTime(2026, 7, 20, 8, 15, 0),
        Tonnage = 1.25m,
        Comments = "Modification de test"
    };

    private static TripEditData CreateEditData(string status = "InProgress") => new()
    {
        TripId = 42,
        TripDate = new DateOnly(2026, 6, 1),
        WorkShiftId = 1,
        TruckId = 1,
        DriverId = 1,
        LoadingPointId = 1,
        DestinationId = 1,
        StartTime = new DateTime(2026, 6, 1, 8, 0, 0),
        EndTime = new DateTime(2026, 6, 1, 8, 20, 0),
        Tonnage = 55m,
        Status = status
    };

    private static TripFormOptions CreateFormOptions() => new()
    {
        Trucks = [new TripFormOption(1, "TRK-001")],
        Drivers = [new TripFormOption(1, "DRV-001 - Conducteur fictif 01")],
        WorkShifts = [new TripFormOption(1, "Poste A (SHIFT-A)")],
        LoadingPoints = [new TripFormOption(1, "LP-01 - Point de chargement 01")],
        Destinations = [new TripFormOption(1, "DEST-PROCESS - Alimentation traitement")]
    };

    private sealed class FakeTripReadService : ITripReadService
    {
        public PagedResult<TripListItemDto> SearchResult { get; init; } =
            PagedResult<TripListItemDto>.Create([], 1, 20, 0);

        public TripDetailsDto? TripDetails { get; init; }

        public TripFilterOptions FilterOptions { get; init; } = new();

        public TripSearchCriteria? LastSearchCriteria { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public Task<PagedResult<TripListItemDto>> SearchAsync(
            TripSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastSearchCriteria = criteria;
            return Task.FromResult(SearchResult);
        }

        public Task<TripDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(TripDetails);
        }

        public Task<TripFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FilterOptions);
        }
    }

    private sealed class FakeTripWriteService : ITripWriteService
    {
        public TripWriteResult CreateResult { get; init; } = TripWriteResult.Succeeded(901);

        public TripWriteResult UpdateResult { get; init; } = TripWriteResult.Succeeded(42);

        public TripEditData? EditData { get; init; }

        public TripFormOptions FormOptions { get; init; } = new();

        public CreateTripCommand? LastCreateCommand { get; private set; }

        public UpdateTripCommand? LastUpdateCommand { get; private set; }

        public int CreateCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public int FormOptionsCallCount { get; private set; }

        public Task<TripWriteResult> CreateAsync(
            CreateTripCommand command,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            LastCreateCommand = command;
            return Task.FromResult(CreateResult);
        }

        public Task<TripEditData?> GetForEditAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EditData);
        }

        public Task<TripWriteResult> UpdateAsync(
            UpdateTripCommand command,
            CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            LastUpdateCommand = command;
            return Task.FromResult(UpdateResult);
        }

        public Task<TripFormOptions> GetFormOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            FormOptionsCallCount++;
            return Task.FromResult(FormOptions);
        }
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
