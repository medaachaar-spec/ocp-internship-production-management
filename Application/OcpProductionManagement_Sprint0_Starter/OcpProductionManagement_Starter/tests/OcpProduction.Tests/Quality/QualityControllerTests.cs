using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OcpProduction.Application.Quality.Commands;
using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Quality;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityControllerTests
{
    [Fact]
    public void Controller_requires_CanManageQuality_and_write_actions_use_antiforgery()
    {
        var authorization = Assert.Single(
            typeof(QualityController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PolicyNames.CanManageQuality, authorization.Policy);

        var actions = typeof(QualityController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var postActions = actions
            .Where(action => action.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();

        Assert.Equal(2, postActions.Length);
        Assert.Contains(postActions, action => action.Name == nameof(QualityController.Create));
        Assert.Contains(postActions, action => action.Name == nameof(QualityController.Edit));
        Assert.All(
            postActions,
            action => Assert.NotNull(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
        Assert.DoesNotContain(actions, action =>
            new[] { "Delete", "Validate", "Submit", "Reject" }.Contains(action.Name));
    }

    [Fact]
    public async Task Index_returns_view_with_normalized_criteria_and_filters()
    {
        var readService = new FakeQualityReadService
        {
            SearchResult = PagedResult<QualityResultListItemDto>.Create(
                [new QualityResultListItemDto { QualityResultId = 17 }],
                page: 1,
                pageSize: 20,
                totalItems: 1),
            FilterOptions = new QualityFilterOptions
            {
                Trucks = [new QualityFilterOption(3, "TRK-003")]
            }
        };
        var controller = CreateController(readService, new FakeQualityWriteService(), false);

        var actionResult = await controller.Index(
            new QualitySearchCriteria
            {
                Search = "  TRK-003  ",
                Page = 0,
                PageSize = 999
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<QualityIndexViewModel>(view.Model);
        Assert.Equal("TRK-003", readService.LastSearchCriteria?.Search);
        Assert.Equal(1, readService.LastSearchCriteria?.Page);
        Assert.Equal(20, readService.LastSearchCriteria?.PageSize);
        Assert.Equal(17, Assert.Single(model.Results.Items).QualityResultId);
        Assert.Equal("17", model.DetailsRouteValues(17)["id"]);
    }

    [Fact]
    public async Task Details_returns_not_found_for_invalid_or_missing_identifier()
    {
        var readService = new FakeQualityReadService();
        var controller = CreateController(readService, new FakeQualityWriteService(), false);

        var invalid = await controller.Details(-1, new QualitySearchCriteria(), CancellationToken.None);
        var missing = await controller.Details(9999, new QualitySearchCriteria(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(invalid);
        Assert.IsType<NotFoundResult>(missing);
        Assert.Equal(1, readService.GetByIdCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Details_respects_CanManageTrips_for_the_related_trip_link(bool isAuthorized)
    {
        var readService = new FakeQualityReadService
        {
            DetailsResult = new QualityResultDetailsDto
            {
                QualityResultId = 17,
                TripId = 42
            }
        };
        var controller = CreateController(
            readService,
            new FakeQualityWriteService(),
            isAuthorized);

        var actionResult = await controller.Details(
            17,
            new QualitySearchCriteria { Search = "  TRK-003 " },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<QualityDetailsViewModel>(view.Model);
        Assert.Equal(17, model.Result.QualityResultId);
        Assert.Equal("TRK-003", model.ReturnCriteria.Search);
        Assert.Equal(isAuthorized, model.CanViewTrip);
    }

    [Fact]
    public async Task Get_Create_returns_verified_form_options()
    {
        var writeService = new FakeQualityWriteService
        {
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);

        var actionResult = await controller.Create(
            new QualitySearchCriteria { Search = "TRK-003" },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<QualityFormViewModel<QualityCreateInputModel>>(view.Model);
        Assert.Equal("TRK-003", model.ReturnCriteria.Search);
        Assert.Equal("LP-01 - Point Nord", Assert.Single(model.Options.LoadingPoints).Label);
        Assert.NotNull(model.Input.AnalysisDate);
    }

    [Fact]
    public async Task Invalid_Create_reloads_options_without_calling_the_write_operation()
    {
        var writeService = new FakeQualityWriteService { FormOptions = CreateFormOptions() };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);
        controller.ModelState.AddModelError("Input.QualityValue", "Valeur invalide.");

        var actionResult = await controller.Create(
            CreateValidCreateInput(),
            new QualitySearchCriteria(),
            CancellationToken.None);

        Assert.IsType<ViewResult>(actionResult);
        Assert.Equal(0, writeService.CreateCallCount);
        Assert.Equal(1, writeService.FormOptionsCallCount);
    }

    [Fact]
    public async Task Valid_Create_redirects_to_details_and_uses_only_the_input_contract()
    {
        var writeService = new FakeQualityWriteService
        {
            CreateResult = QualityWriteResult.Succeeded(501)
        };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);

        var actionResult = await controller.Create(
            CreateValidCreateInput(),
            new QualitySearchCriteria { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal(nameof(QualityController.Details), redirect.ActionName);
        Assert.Equal("501", redirect.RouteValues!["id"]);
        Assert.Equal("Le résultat qualité a été enregistré avec succès.", controller.TempData["Success"]);
        Assert.Equal(30.125m, writeService.LastCreateCommand?.QualityValue);
    }

    [Fact]
    public async Task Get_Edit_returns_existing_measurement_without_protected_fields_in_the_input()
    {
        var writeService = new FakeQualityWriteService
        {
            EditData = CreateEditData(),
            FormOptions = CreateFormOptions()
        };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);

        var actionResult = await controller.Edit(
            17,
            new QualitySearchCriteria { PageSize = 10 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<QualityFormViewModel<QualityEditInputModel>>(view.Model);
        Assert.Equal(17, model.Input.QualityResultId);
        Assert.Equal(30.125m, model.Input.QualityValue);
        Assert.Equal(QualityStatusDefinitions.Review, model.CurrentStatus);

        var propertyNames = typeof(QualityEditInputModel)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain("Status", propertyNames);
        Assert.DoesNotContain("TargetValue", propertyNames);
        Assert.DoesNotContain("CreatedAt", propertyNames);
        Assert.DoesNotContain("QualityUnit", propertyNames);
    }

    [Fact]
    public async Task Post_Edit_rejects_a_forged_mismatched_identifier()
    {
        var writeService = new FakeQualityWriteService { EditData = CreateEditData() };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);

        var actionResult = await controller.Edit(
            18,
            CreateValidEditInput(),
            new QualitySearchCriteria(),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(actionResult);
        Assert.Equal(0, writeService.UpdateCallCount);
    }

    [Fact]
    public async Task Valid_Edit_redirects_to_details_and_sets_a_safe_message()
    {
        var writeService = new FakeQualityWriteService
        {
            EditData = CreateEditData(),
            UpdateResult = QualityWriteResult.Succeeded(17)
        };
        var controller = CreateController(new FakeQualityReadService(), writeService, false);

        var actionResult = await controller.Edit(
            17,
            CreateValidEditInput(),
            new QualitySearchCriteria { Search = "TRK-003" },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal(nameof(QualityController.Details), redirect.ActionName);
        Assert.Equal("17", redirect.RouteValues!["id"]);
        Assert.Equal("Le résultat qualité a été modifié avec succès.", controller.TempData["Success"]);
        Assert.Equal(17, writeService.LastUpdateCommand?.QualityResultId);
    }

    private static QualityController CreateController(
        IQualityReadService readService,
        IQualityWriteService writeService,
        bool isTripAuthorized)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        return new QualityController(
            readService,
            writeService,
            new FakeAuthorizationService(isTripAuthorized))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static QualityCreateInputModel CreateValidCreateInput() => new()
    {
        AnalysisDate = new DateTime(2026, 7, 20, 15, 30, 0),
        TripId = 42,
        LoadingPointId = 1,
        QualityValue = 30.125m,
        Comments = "Analyse de test"
    };

    private static QualityEditInputModel CreateValidEditInput() => new()
    {
        QualityResultId = 17,
        AnalysisDate = new DateTime(2026, 7, 20, 15, 30, 0),
        TripId = 42,
        LoadingPointId = 1,
        QualityValue = 30.250m,
        Comments = "Analyse corrigée"
    };

    private static QualityEditData CreateEditData() => new()
    {
        QualityResultId = 17,
        AnalysisDate = new DateTime(2026, 7, 20, 15, 0, 0),
        TripId = 42,
        LoadingPointId = 1,
        QualityValue = 30.125m,
        Status = QualityStatusDefinitions.Review,
        Comments = "Analyse existante"
    };

    private static QualityFormOptions CreateFormOptions() => new()
    {
        LoadingPoints = [new QualityLoadingPointOption(1, "LP-01 - Point Nord")]
    };

    private sealed class FakeQualityReadService : IQualityReadService
    {
        public PagedResult<QualityResultListItemDto> SearchResult { get; init; } =
            PagedResult<QualityResultListItemDto>.Create([], 1, 20, 0);

        public QualityResultDetailsDto? DetailsResult { get; init; }

        public QualityFilterOptions FilterOptions { get; init; } = new();

        public QualitySearchCriteria? LastSearchCriteria { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public Task<PagedResult<QualityResultListItemDto>> SearchAsync(
            QualitySearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastSearchCriteria = criteria;
            return Task.FromResult(SearchResult);
        }

        public Task<QualityResultDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(DetailsResult);
        }

        public Task<QualityFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(FilterOptions);
    }

    private sealed class FakeQualityWriteService : IQualityWriteService
    {
        public QualityWriteResult CreateResult { get; init; } = QualityWriteResult.Succeeded(501);

        public QualityWriteResult UpdateResult { get; init; } = QualityWriteResult.Succeeded(17);

        public QualityEditData? EditData { get; init; }

        public QualityFormOptions FormOptions { get; init; } = new();

        public CreateQualityResultCommand? LastCreateCommand { get; private set; }

        public UpdateQualityResultCommand? LastUpdateCommand { get; private set; }

        public int CreateCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public int FormOptionsCallCount { get; private set; }

        public Task<QualityWriteResult> CreateAsync(
            CreateQualityResultCommand command,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            LastCreateCommand = command;
            return Task.FromResult(CreateResult);
        }

        public Task<QualityEditData?> GetForEditAsync(
            long id,
            CancellationToken cancellationToken = default) => Task.FromResult(EditData);

        public Task<QualityWriteResult> UpdateAsync(
            UpdateQualityResultCommand command,
            CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            LastUpdateCommand = command;
            return Task.FromResult(UpdateResult);
        }

        public Task<QualityFormOptions> GetFormOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            FormOptionsCallCount++;
            return Task.FromResult(FormOptions);
        }
    }

    private sealed class FakeAuthorizationService(bool succeeds) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(succeeds ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            Assert.Equal(PolicyNames.CanManageTrips, policyName);
            return Task.FromResult(succeeds ? AuthorizationResult.Success() : AuthorizationResult.Failed());
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
