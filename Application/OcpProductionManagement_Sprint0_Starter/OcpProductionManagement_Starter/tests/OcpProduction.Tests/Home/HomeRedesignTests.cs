using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.Security;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels;
using Xunit;

namespace OcpProduction.Tests.Home;

public sealed class HomeRedesignTests
{
    [Fact]
    public async Task Supervisor_home_uses_authorized_read_only_results_for_summary_and_activity()
    {
        var tripReadService = new FakeTripReadService
        {
            Result = PagedResult<TripListItemDto>.Create(
                Enumerable.Range(1, 6)
                    .Select(id => new TripListItemDto { TripId = id })
                    .ToArray(),
                1,
                10,
                7)
        };
        var stopReadService = new FakeStopReadService
        {
            Result = PagedResult<StopListItemDto>.Create(
                [new StopListItemDto
                {
                    StopId = 8,
                    TruckCode = "TRK-008",
                    StopReasonCode = "MEC",
                    StopReasonName = "Maintenance",
                    State = StopStates.Open,
                    ValidationStatus = StopValidationStatuses.Pending
                }],
                1,
                10,
                3)
        };
        var qualityReadService = new FakeQualityReadService
        {
            Result = PagedResult<QualityResultListItemDto>.Create(
                [new QualityResultListItemDto { QualityResultId = 12 }],
                1,
                10,
                4)
        };
        var tripWorkflowService = new FakeTripWorkflowService { PendingCount = 2 };
        var stopWorkflowService = new FakeStopWorkflowService { PendingCount = 1 };
        var controller = CreateController(
            AppRoles.Superviseur,
            [
                PolicyNames.CanManageTrips,
                PolicyNames.CanManageStops,
                PolicyNames.CanManageQuality,
                PolicyNames.CanValidate,
                PolicyNames.CanViewDashboard
            ],
            tripReadService,
            stopReadService,
            qualityReadService,
            tripWorkflowService,
            stopWorkflowService);

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeDashboardViewModel>(view.Model);
        Assert.True(model.CanManageTrips);
        Assert.True(model.CanManageStops);
        Assert.True(model.CanManageQuality);
        Assert.True(model.CanValidate);
        Assert.True(model.CanViewDashboard);
        Assert.False(model.CanAdminister);
        Assert.False(model.CanViewTechnicalDiagnostics);
        Assert.Equal("superviseur", model.RoleDisplayName);
        Assert.Equal(7, model.TripsTodayCount);
        Assert.Equal(3, model.OpenStopsCount);
        Assert.Equal(4, model.QualityAnalysesTodayCount);
        Assert.Equal(2, model.PendingValidationCount);
        Assert.Equal(1, model.PendingStopValidationCount);
        Assert.Equal(5, model.RecentTrips.Count);
        Assert.Single(model.RecentOpenStops);
        Assert.Single(model.RecentQualityAnalyses);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), tripReadService.LastCriteria?.DateFrom);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), tripReadService.LastCriteria?.DateTo);
        Assert.Equal(StopStates.Open, stopReadService.LastCriteria?.State);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), qualityReadService.LastCriteria?.DateFrom);
        Assert.Equal(1, tripWorkflowService.PendingCountCalls);
        Assert.Equal(1, stopWorkflowService.PendingCountCalls);
    }

    [Fact]
    public async Task Administrator_home_hides_unauthorized_operational_actions_and_queries()
    {
        var tripReadService = new FakeTripReadService();
        var stopReadService = new FakeStopReadService();
        var qualityReadService = new FakeQualityReadService();
        var tripWorkflowService = new FakeTripWorkflowService();
        var stopWorkflowService = new FakeStopWorkflowService();
        var controller = CreateController(
            AppRoles.Administrateur,
            [
                PolicyNames.CanViewDashboard,
                PolicyNames.CanAdminister,
                PolicyNames.CanViewTechnicalDiagnostics
            ],
            tripReadService,
            stopReadService,
            qualityReadService,
            tripWorkflowService,
            stopWorkflowService);

        var result = await controller.Index(CancellationToken.None);

        var model = Assert.IsType<HomeDashboardViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.False(model.CanManageTrips);
        Assert.False(model.CanManageStops);
        Assert.False(model.CanManageQuality);
        Assert.False(model.CanValidate);
        Assert.True(model.CanViewDashboard);
        Assert.True(model.CanAdminister);
        Assert.True(model.CanViewTechnicalDiagnostics);
        Assert.Equal("administrateur", model.RoleDisplayName);
        Assert.False(model.HasOperationalSummary);
        Assert.False(model.HasRecentActivity);
        Assert.Null(tripReadService.LastCriteria);
        Assert.Null(stopReadService.LastCriteria);
        Assert.Null(qualityReadService.LastCriteria);
        Assert.Equal(0, tripWorkflowService.PendingCountCalls);
        Assert.Equal(0, stopWorkflowService.PendingCountCalls);
    }

    [Fact]
    public void Home_view_is_professional_policy_gated_responsive_and_accessible()
    {
        var view = File.ReadAllText(GetWebFile("Views", "Home", "Index.cshtml"));
        var layout = File.ReadAllText(GetWebFile("Views", "Shared", "_Layout.cshtml"));
        var css = File.ReadAllText(GetWebFile("wwwroot", "css", "site.css"));
        var homeCss = css[css.IndexOf(".home-page", StringComparison.Ordinal)..];

        Assert.Contains("<h1>Accueil</h1>", view, StringComparison.Ordinal);
        Assert.Contains("Gestion de la production minière — Phosboucraa", view, StringComparison.Ordinal);
        Assert.Contains("home-welcome-panel", view, StringComparison.Ordinal);
        Assert.Contains("Accès rapides", view, StringComparison.Ordinal);
        Assert.Contains("Synthèse opérationnelle", view, StringComparison.Ordinal);
        Assert.Contains("Activité récente", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanManageTrips)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanManageStops)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanManageQuality)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanValidate)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewDashboard)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanAdminister)", view, StringComparison.Ordinal);
        Assert.Contains("@if (Model.CanViewTechnicalDiagnostics)", view, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=", view, StringComparison.Ordinal);
        Assert.Contains("<time datetime=", view, StringComparison.Ordinal);
        Assert.Contains("home-action-card--green", view, StringComparison.Ordinal);
        Assert.Contains("home-action-card--blue", view, StringComparison.Ordinal);
        Assert.Contains("home-action-card--beige", view, StringComparison.Ordinal);
        Assert.Contains("home-metric-card--deep-green", view, StringComparison.Ordinal);
        Assert.Contains("home-activity-card--green", view, StringComparison.Ordinal);
        Assert.Contains(".home-welcome-panel", homeCss, StringComparison.Ordinal);
        Assert.Contains("border-left: 4px solid var(--ocp-green)", homeCss, StringComparison.Ordinal);
        Assert.Contains("--home-accent", homeCss, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", homeCss, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 576px)", homeCss, StringComparison.Ordinal);
        Assert.Contains("minmax(min(", homeCss, StringComparison.Ordinal);
        Assert.DoesNotContain("gradient", homeCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sprint 0", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Prochaine livraison", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("scaffolding", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cookie", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Prototype", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("marketing", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("intelligence artificielle", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Home_controller_introduces_no_write_operation()
    {
        var source = File.ReadAllText(GetWebFile("Controllers", "HomeController.cs"));

        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ITripWriteService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IStopWriteService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IQualityWriteService", source, StringComparison.Ordinal);
    }

    private static HomeController CreateController(
        string role,
        IReadOnlyCollection<string> allowedPolicies,
        ITripReadService tripReadService,
        IStopReadService stopReadService,
        IQualityReadService qualityReadService,
        ITripWorkflowService tripWorkflowService,
        IStopWorkflowService stopWorkflowService)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "Utilisateur test"),
                new Claim(ClaimTypes.Role, role)
            ],
            "Test");
        var controller = new HomeController(
            new FakeAuthorizationService(allowedPolicies),
            tripReadService,
            stopReadService,
            qualityReadService,
            tripWorkflowService,
            stopWorkflowService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        return controller;
    }

    private static string GetWebFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OcpProductionManagement.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(
            directory!.FullName,
            Path.Combine(["src", "OcpProduction.Web", .. segments]));
    }

    private sealed class FakeAuthorizationService(
        IReadOnlyCollection<string> allowedPolicies) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName) =>
            Task.FromResult(allowedPolicies.Contains(policyName)
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed());
    }

    private sealed class FakeTripReadService : ITripReadService
    {
        public PagedResult<TripListItemDto> Result { get; init; } =
            PagedResult<TripListItemDto>.Create([], 1, 10, 0);

        public TripSearchCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<TripListItemDto>> SearchAsync(
            TripSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(Result);
        }

        public Task<TripDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) => Task.FromResult<TripDetailsDto?>(null);

        public Task<TripFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(new TripFilterOptions());
    }

    private sealed class FakeStopReadService : IStopReadService
    {
        public PagedResult<StopListItemDto> Result { get; init; } =
            PagedResult<StopListItemDto>.Create([], 1, 10, 0);

        public StopSearchCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<StopListItemDto>> SearchAsync(
            StopSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(Result);
        }

        public Task<StopDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) => Task.FromResult<StopDetailsDto?>(null);

        public Task<StopFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(new StopFilterOptions());
    }

    private sealed class FakeQualityReadService : IQualityReadService
    {
        public PagedResult<QualityResultListItemDto> Result { get; init; } =
            PagedResult<QualityResultListItemDto>.Create([], 1, 10, 0);

        public QualitySearchCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<QualityResultListItemDto>> SearchAsync(
            QualitySearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(Result);
        }

        public Task<QualityResultDetailsDto?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<QualityResultDetailsDto?>(null);

        public Task<QualityFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QualityFilterOptions());
    }

    private sealed class FakeTripWorkflowService : ITripWorkflowService
    {
        public long PendingCount { get; init; }

        public int PendingCountCalls { get; private set; }

        public Task<long> GetPendingValidationCountAsync(
            CancellationToken cancellationToken = default)
        {
            PendingCountCalls++;
            return Task.FromResult(PendingCount);
        }

        public Task<TripClosureData?> GetForClosureAsync(
            long tripId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TripClosureData?>(null);

        public Task<TripWorkflowResult> CloseAsync(
            CloseTripCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TripWorkflowResult.NotFound(command.TripId));

        public Task<PagedResult<PendingTripListItemDto>> GetPendingValidationAsync(
            TripValidationSearchCriteria criteria,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<PendingTripListItemDto>.Create([], 1, 20, 0));

        public Task<TripWorkflowResult> ValidateAsync(
            long tripId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TripWorkflowResult.NotFound(tripId));
    }

    private sealed class FakeStopWorkflowService : IStopWorkflowService
    {
        public long PendingCount { get; init; }

        public int PendingCountCalls { get; private set; }

        public Task<long> GetPendingValidationCountAsync(
            CancellationToken cancellationToken = default)
        {
            PendingCountCalls++;
            return Task.FromResult(PendingCount);
        }

        public Task<StopClosureData?> GetForClosureAsync(
            long stopId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StopClosureData?>(null);

        public Task<StopWorkflowResult> CloseAsync(
            CloseStopCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StopWorkflowResult.NotFound(command.StopId));

        public Task<PagedResult<PendingStopListItemDto>> GetPendingValidationAsync(
            StopValidationSearchCriteria criteria,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<PendingStopListItemDto>.Create([], 1, 20, 0));

        public Task<StopWorkflowResult> ValidateAsync(
            long stopId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StopWorkflowResult.NotFound(stopId));
    }
}
