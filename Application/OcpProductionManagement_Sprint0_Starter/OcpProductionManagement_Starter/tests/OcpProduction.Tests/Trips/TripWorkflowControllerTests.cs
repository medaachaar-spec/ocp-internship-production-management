using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripWorkflowControllerTests
{
    [Fact]
    public void Workflow_endpoints_use_the_documented_policies_and_antiforgery()
    {
        var closeAuthorization = Assert.Single(
            typeof(TripWorkflowController).GetCustomAttributes<AuthorizeAttribute>());
        var validationAuthorization = Assert.Single(
            typeof(ValidationController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(PolicyNames.CanManageTrips, closeAuthorization.Policy);
        Assert.Equal(PolicyNames.CanValidate, validationAuthorization.Policy);

        var closePost = Assert.Single(
            typeof(TripWorkflowController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.GetCustomAttribute<HttpPostAttribute>() is not null);
        var validatePost = Assert.Single(
            typeof(ValidationController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.GetCustomAttribute<HttpPostAttribute>() is not null);

        Assert.NotNull(closePost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(validatePost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(nameof(TripWorkflowController.Close), closePost.Name);
        Assert.Equal(nameof(ValidationController.Validate), validatePost.Name);
    }

    [Fact]
    public void No_reject_reopen_cancel_or_delete_endpoint_exists()
    {
        var actions = new[] { typeof(TripWorkflowController), typeof(ValidationController) }
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));

        Assert.DoesNotContain(
            actions,
            action => new[] { "Reject", "Reopen", "Cancel", "Delete" }.Contains(action.Name));
    }

    [Fact]
    public async Task Get_Close_returns_confirmation_only_for_an_InProgress_trip()
    {
        var service = new FakeTripWorkflowService { ClosureData = CreateClosureData() };
        var controller = CreateCloseController(service);

        var result = await controller.Close(
            42,
            new TripSearchCriteria { Search = "TRK-001" },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TripCloseViewModel>(view.Model);
        Assert.Equal(42, model.Input.TripId);
        Assert.Equal("TRK-001", model.ReturnCriteria.Search);
    }

    [Fact]
    public async Task Post_Close_rejects_route_form_mismatch_and_invalid_model()
    {
        var service = new FakeTripWorkflowService { ClosureData = CreateClosureData() };
        var mismatchController = CreateCloseController(service);
        var mismatch = await mismatchController.Close(
            41,
            CreateValidInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(mismatch);
        Assert.Equal(0, service.CloseCallCount);

        var invalidController = CreateCloseController(service);
        invalidController.ModelState.AddModelError("Input.Tonnage", "Le tonnage est obligatoire.");
        var invalid = await invalidController.Close(
            42,
            CreateValidInput(),
            new TripSearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(invalid);
        Assert.Equal(0, service.CloseCallCount);
    }

    [Fact]
    public async Task Valid_Close_redirects_to_details_with_safe_success_message()
    {
        var service = new FakeTripWorkflowService
        {
            ClosureData = CreateClosureData(),
            CloseResult = TripWorkflowResult.Succeeded(42)
        };
        var controller = CreateCloseController(service);

        var result = await controller.Close(
            42,
            CreateValidInput(),
            new TripSearchCriteria { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal("Trips", redirect.ControllerName);
        Assert.Equal("42", redirect.RouteValues!["id"]);
        Assert.Equal("Le voyage a été clôturé et transmis pour validation.", controller.TempData["Success"]);
        Assert.Equal(42, service.LastCloseCommand?.TripId);
    }

    [Fact]
    public async Task Pending_queue_and_validation_use_the_workflow_service()
    {
        var service = new FakeTripWorkflowService
        {
            PendingResult = PagedResult<PendingTripListItemDto>.Create(
                [new PendingTripListItemDto { TripId = 42 }],
                1,
                20,
                1),
            ValidateResult = TripWorkflowResult.Succeeded(42)
        };
        var controller = CreateValidationController(service);

        var queueResult = await controller.Index(
            new TripValidationSearchCriteria { Search = "  TRK-001  " },
            CancellationToken.None);
        var queueView = Assert.IsType<ViewResult>(queueResult);
        var queueModel = Assert.IsType<PendingValidationIndexViewModel>(queueView.Model);
        Assert.Equal("TRK-001", queueModel.Criteria.Search);
        Assert.Equal(42, Assert.Single(queueModel.Results.Items).TripId);

        var validationResult = await controller.Validate(42, CancellationToken.None);
        var redirect = Assert.IsType<RedirectToActionResult>(validationResult);
        Assert.Equal(nameof(ValidationController.Index), redirect.ActionName);
        Assert.Equal("Le voyage a été validé avec succès.", controller.TempData["Success"]);
        Assert.Equal(42, service.LastValidatedTripId);
    }

    [Fact]
    public async Task Invalid_validation_state_redirects_to_safe_review_message()
    {
        var service = new FakeTripWorkflowService
        {
            ValidateResult = TripWorkflowResult.InvalidState(
                42,
                "Ce voyage a déjà été traité ou son état a changé.")
        };
        var controller = CreateValidationController(service);

        var result = await controller.Validate(42, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TripsController.Details), redirect.ActionName);
        Assert.Equal("Trips", redirect.ControllerName);
        Assert.Equal("Ce voyage a déjà été traité ou son état a changé.", controller.TempData["Error"]);
    }

    private static TripWorkflowController CreateCloseController(ITripWorkflowService service)
    {
        var httpContext = new DefaultHttpContext();
        return new TripWorkflowController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static ValidationController CreateValidationController(ITripWorkflowService service)
    {
        var httpContext = new DefaultHttpContext();
        return new ValidationController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static TripClosureData CreateClosureData() => new()
    {
        TripId = 42,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        Status = "InProgress",
        TruckCode = "TRK-001"
    };

    private static TripCloseInputModel CreateValidInput() => new()
    {
        TripId = 42,
        EndTime = new DateTime(2026, 7, 20, 9, 0, 0),
        Tonnage = 1.25m
    };

    private sealed class FakeTripWorkflowService : ITripWorkflowService
    {
        public TripClosureData? ClosureData { get; init; }

        public TripWorkflowResult CloseResult { get; init; } = TripWorkflowResult.Succeeded(42);

        public PagedResult<PendingTripListItemDto> PendingResult { get; init; } =
            PagedResult<PendingTripListItemDto>.Create([], 1, 20, 0);

        public TripWorkflowResult ValidateResult { get; init; } = TripWorkflowResult.Succeeded(42);

        public CloseTripCommand? LastCloseCommand { get; private set; }

        public long? LastValidatedTripId { get; private set; }

        public int CloseCallCount { get; private set; }

        public Task<TripClosureData?> GetForClosureAsync(
            long tripId,
            CancellationToken cancellationToken = default) => Task.FromResult(ClosureData);

        public Task<TripWorkflowResult> CloseAsync(
            CloseTripCommand command,
            CancellationToken cancellationToken = default)
        {
            CloseCallCount++;
            LastCloseCommand = command;
            return Task.FromResult(CloseResult);
        }

        public Task<PagedResult<PendingTripListItemDto>> GetPendingValidationAsync(
            TripValidationSearchCriteria criteria,
            CancellationToken cancellationToken = default) => Task.FromResult(PendingResult);

        public Task<long> GetPendingValidationCountAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(PendingResult.TotalItems);

        public Task<TripWorkflowResult> ValidateAsync(
            long tripId,
            CancellationToken cancellationToken = default)
        {
            LastValidatedTripId = tripId;
            return Task.FromResult(ValidateResult);
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
