using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Application.Dashboard.Services;
using OcpProduction.Application.Security;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Dashboard;
using Xunit;

namespace OcpProduction.Tests.Dashboard;

public sealed class DashboardControllerTests
{
    [Fact]
    public void Controller_uses_CanViewDashboard_and_exposes_get_only_action()
    {
        var authorize = Assert.Single(
            typeof(DashboardController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PolicyNames.CanViewDashboard, authorize.Policy);

        var action = Assert.Single(
            typeof(DashboardController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public),
            method => method.DeclaringType == typeof(DashboardController));

        Assert.Equal(nameof(DashboardController.Index), action.Name);
        Assert.NotNull(action.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(action.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task Invalid_date_range_does_not_query_reporting_service()
    {
        var service = new FakeDashboardReadService();
        var controller = CreateController(service);
        var criteria = new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 6, 30)
        };

        var result = await controller.Index(criteria, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(0, service.CallCount);
        Assert.False(controller.ModelState.IsValid);
        Assert.Same(criteria, model.Criteria);
        Assert.False(model.HasLoadError);
        Assert.False(model.HasAnyData);
    }

    [Fact]
    public async Task Valid_criteria_query_once_and_map_service_result()
    {
        var expected = new DashboardDto
        {
            TripCount = 800,
            TotalTonnage = 54473.80m,
            StopCount = 149,
            TotalStopDurationHours = 415.50m,
            QualityAnalysisCount = 120,
            PendingValidationTripCount = 0
        };
        var service = new FakeDashboardReadService(expected);
        var controller = CreateController(service);
        var criteria = new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30)
        };

        var result = await controller.Index(criteria, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(1, service.CallCount);
        Assert.Same(criteria, service.LastCriteria);
        Assert.Same(expected, model.Kpis);
        Assert.Same(criteria, model.Criteria);
        Assert.False(model.HasLoadError);
        Assert.True(model.HasAnyData);
    }

    [Fact]
    public async Task Query_exception_returns_safe_error_model_and_preserves_dates()
    {
        var service = new FakeDashboardReadService(
            new InvalidOperationException("Sensitive database detail"));
        var controller = CreateController(service);
        var criteria = new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30)
        };

        var result = await controller.Index(criteria, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(1, service.CallCount);
        Assert.True(model.HasLoadError);
        Assert.Same(criteria, model.Criteria);
        Assert.False(model.HasAnyData);
        Assert.Empty(controller.ModelState);
    }

    [Fact]
    public async Task Empty_service_result_is_a_valid_zero_data_state()
    {
        var controller = CreateController(new FakeDashboardReadService(new DashboardDto()));

        var result = await controller.Index(
            new DashboardDateCriteria
            {
                StartDate = new DateOnly(2030, 1, 1),
                EndDate = new DateOnly(2030, 1, 31)
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.False(model.HasLoadError);
        Assert.False(model.HasAnyData);
        Assert.Equal(0, model.Kpis.TripCount);
        Assert.Equal(0m, model.Kpis.TotalTonnage);
        Assert.Equal(0, model.Kpis.StopCount);
        Assert.Equal(0m, model.Kpis.TotalStopDurationHours);
        Assert.Equal(0, model.Kpis.QualityAnalysisCount);
        Assert.Equal(0, model.Kpis.PendingValidationTripCount);
    }

    private static DashboardController CreateController(IDashboardReadService service)
    {
        return new DashboardController(
            service,
            NullLogger<DashboardController>.Instance);
    }

    private sealed class FakeDashboardReadService : IDashboardReadService
    {
        private readonly DashboardDto _result = new();
        private readonly Exception? _exception;

        public FakeDashboardReadService()
        {
        }

        public FakeDashboardReadService(DashboardDto result)
        {
            _result = result;
        }

        public FakeDashboardReadService(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public DashboardDateCriteria? LastCriteria { get; private set; }

        public Task<DashboardDto> GetDashboardAsync(
            DashboardDateCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCriteria = criteria;

            if (_exception is not null)
            {
                return Task.FromException<DashboardDto>(_exception);
            }

            return Task.FromResult(_result);
        }
    }
}
