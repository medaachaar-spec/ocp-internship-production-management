using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Performance;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformanceControllerTests
{
    [Fact]
    public void Controller_uses_CanViewReporting_and_exposes_get_only_actions()
    {
        var authorize = Assert.Single(typeof(PerformanceController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PolicyNames.CanViewReporting, authorize.Policy);

        var actions = typeof(PerformanceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType == typeof(PerformanceController))
            .ToList();

        Assert.All(actions, action => Assert.NotNull(action.GetCustomAttribute<HttpGetAttribute>()));
        Assert.DoesNotContain(actions, action => action.GetCustomAttribute<HttpPostAttribute>() is not null);
        Assert.DoesNotContain(actions, action => action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() is not null);
    }

    [Fact]
    public async Task Trucks_action_normalizes_filters_and_returns_results()
    {
        var service = new FakePerformanceReadService();
        var controller = new PerformanceController(service);

        var result = await controller.Trucks(
            new TruckPerformanceSearchCriteria { Search = "  TRK-001  ", Page = 0, PageSize = 999 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TruckPerformanceIndexViewModel>(view.Model);
        Assert.Equal("TRK-001", service.LastTruckCriteria?.Search);
        Assert.Equal(20, service.LastTruckCriteria?.PageSize);
        Assert.Single(model.Results.Items);
    }

    [Fact]
    public async Task Drivers_action_normalizes_filters_and_returns_results()
    {
        var service = new FakePerformanceReadService();
        var controller = new PerformanceController(service);

        var result = await controller.Drivers(
            new DriverPerformanceSearchCriteria { Search = "  Conducteur 01  ", Sort = "Bad" },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DriverPerformanceIndexViewModel>(view.Model);
        Assert.Equal("Conducteur 01", service.LastDriverCriteria?.Search);
        Assert.Equal(DriverPerformanceSort.TotalTripsDesc, service.LastDriverCriteria?.Sort);
        Assert.Single(model.Results.Items);
    }

    [Fact]
    public async Task Detail_actions_return_not_found_for_invalid_or_missing_ids()
    {
        var controller = new PerformanceController(new FakePerformanceReadService());

        Assert.IsType<NotFoundResult>(await controller.TruckDetails(0, new TruckPerformanceSearchCriteria(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.TruckDetails(999, new TruckPerformanceSearchCriteria(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.DriverDetails(0, new DriverPerformanceSearchCriteria(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.DriverDetails(999, new DriverPerformanceSearchCriteria(), CancellationToken.None));
    }

    private sealed class FakePerformanceReadService : IPerformanceReadService
    {
        public TruckPerformanceSearchCriteria? LastTruckCriteria { get; private set; }
        public DriverPerformanceSearchCriteria? LastDriverCriteria { get; private set; }

        public Task<PagedResult<TruckPerformanceListItemDto>> SearchTrucksAsync(
            TruckPerformanceSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastTruckCriteria = criteria;
            return Task.FromResult(PagedResult<TruckPerformanceListItemDto>.Create(
                [new TruckPerformanceListItemDto { TruckId = 1, TruckCode = "TRK-001", TruckStatus = "InService" }],
                criteria.Page,
                criteria.PageSize,
                1));
        }

        public Task<TruckPerformanceDetailsDto?> GetTruckAsync(
            int truckId,
            CancellationToken cancellationToken = default)
        {
            TruckPerformanceDetailsDto? result = truckId == 1
                ? new TruckPerformanceDetailsDto { TruckId = 1, TruckCode = "TRK-001", TruckStatus = "InService" }
                : null;
            return Task.FromResult(result);
        }

        public Task<PagedResult<DriverPerformanceListItemDto>> SearchDriversAsync(
            DriverPerformanceSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastDriverCriteria = criteria;
            return Task.FromResult(PagedResult<DriverPerformanceListItemDto>.Create(
                [new DriverPerformanceListItemDto { DriverId = 1, DriverCode = "DRV-001", DriverFullName = "Conducteur 01", DriverStatus = "Active" }],
                criteria.Page,
                criteria.PageSize,
                1));
        }

        public Task<DriverPerformanceDetailsDto?> GetDriverAsync(
            int driverId,
            CancellationToken cancellationToken = default)
        {
            DriverPerformanceDetailsDto? result = driverId == 1
                ? new DriverPerformanceDetailsDto { DriverId = 1, DriverCode = "DRV-001", DriverFullName = "Conducteur 01", DriverStatus = "Active" }
                : null;
            return Task.FromResult(result);
        }

        public Task<PerformanceFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PerformanceFilterOptions
            {
                TruckStatuses = ["InService"],
                DriverStatuses = ["Active"]
            });
        }
    }
}
