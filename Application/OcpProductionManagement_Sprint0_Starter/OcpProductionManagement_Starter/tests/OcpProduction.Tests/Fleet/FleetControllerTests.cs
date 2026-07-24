using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Application.Drivers.Dtos;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Drivers.Services;
using OcpProduction.Application.Security;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trucks.Dtos;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Application.Trucks.Services;
using OcpProduction.Web.Controllers;
using OcpProduction.Web.ViewModels.Drivers;
using OcpProduction.Web.ViewModels.Trucks;
using Xunit;

namespace OcpProduction.Tests.Fleet;

public sealed class FleetControllerTests
{
    [Theory]
    [InlineData(typeof(TrucksController))]
    [InlineData(typeof(DriversController))]
    public void Fleet_controllers_use_the_documented_read_roles_and_get_only_actions(Type controllerType)
    {
        var authorize = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(FleetAccessRoles.All, authorize.Roles);
        Assert.Null(authorize.Policy);

        var actionMethods = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType == controllerType)
            .ToList();

        Assert.All(actionMethods, method => Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>()));
        Assert.DoesNotContain(actionMethods, method => method.GetCustomAttribute<HttpPostAttribute>() is not null);
        Assert.DoesNotContain(actionMethods, method => method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() is not null);
    }

    [Fact]
    public async Task Truck_index_normalizes_filters_and_returns_filter_options()
    {
        var service = new FakeTruckReadService();
        var controller = CreateTruckController(service, canViewRelatedData: false);

        var result = await controller.Index(
            new TruckSearchCriteria { Search = "  TRK-001  ", Page = 0, PageSize = 999 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TruckIndexViewModel>(view.Model);
        Assert.Equal("TRK-001", service.LastCriteria?.Search);
        Assert.Equal(1, service.LastCriteria?.Page);
        Assert.Equal(20, service.LastCriteria?.PageSize);
        Assert.Contains(TruckStatusDefinitions.InService, model.FilterOptions.Statuses);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Truck_details_respects_related_trip_and_stop_permissions(bool isAuthorized)
    {
        var controller = CreateTruckController(new FakeTruckReadService(), isAuthorized);

        var result = await controller.Details(1, new TruckSearchCriteria(), CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TruckDetailsViewModel>(view.Model);
        Assert.Equal(isAuthorized, model.CanViewTrips);
        Assert.Equal(isAuthorized, model.CanViewStops);
    }

    [Fact]
    public async Task Truck_details_returns_not_found_for_invalid_or_missing_ids()
    {
        var controller = CreateTruckController(new FakeTruckReadService(), canViewRelatedData: true);

        Assert.IsType<NotFoundResult>(await controller.Details(0, new TruckSearchCriteria(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Details(999, new TruckSearchCriteria(), CancellationToken.None));
    }

    [Fact]
    public async Task Driver_index_normalizes_filters_and_returns_filter_options()
    {
        var service = new FakeDriverReadService();
        var controller = CreateDriverController(service, canViewRelatedData: false);

        var result = await controller.Index(
            new DriverSearchCriteria { Search = "  Conducteur 01  ", Page = 0, PageSize = 999 },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DriverIndexViewModel>(view.Model);
        Assert.Equal("Conducteur 01", service.LastCriteria?.Search);
        Assert.Equal(1, service.LastCriteria?.Page);
        Assert.Equal(20, service.LastCriteria?.PageSize);
        Assert.Contains(DriverStatusDefinitions.Active, model.FilterOptions.Statuses);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Driver_details_respects_related_trip_and_stop_permissions(bool isAuthorized)
    {
        var controller = CreateDriverController(new FakeDriverReadService(), isAuthorized);

        var result = await controller.Details(1, new DriverSearchCriteria(), CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DriverDetailsViewModel>(view.Model);
        Assert.Equal(isAuthorized, model.CanViewTrips);
        Assert.Equal(isAuthorized, model.CanViewStops);
    }

    [Fact]
    public async Task Driver_details_returns_not_found_for_invalid_or_missing_ids()
    {
        var controller = CreateDriverController(new FakeDriverReadService(), canViewRelatedData: true);

        Assert.IsType<NotFoundResult>(await controller.Details(0, new DriverSearchCriteria(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Details(999, new DriverSearchCriteria(), CancellationToken.None));
    }

    private static TrucksController CreateTruckController(
        ITruckReadService service,
        bool canViewRelatedData)
    {
        return new TrucksController(service, new FakeAuthorizationService(canViewRelatedData))
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };
    }

    private static DriversController CreateDriverController(
        IDriverReadService service,
        bool canViewRelatedData)
    {
        return new DriversController(service, new FakeAuthorizationService(canViewRelatedData))
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "fleet-test")],
                "Test"))
        };
    }

    private sealed class FakeAuthorizationService(bool isAuthorized) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            Assert.Contains(policyName, new[] { PolicyNames.CanManageTrips, PolicyNames.CanManageStops });
            return Task.FromResult(isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }
    }

    private sealed class FakeTruckReadService : ITruckReadService
    {
        public TruckSearchCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<TruckListItemDto>> SearchAsync(
            TruckSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(PagedResult<TruckListItemDto>.Create(
                [new TruckListItemDto { TruckId = 1, TruckCode = "TRK-001", Status = TruckStatusDefinitions.InService }],
                criteria.Page,
                criteria.PageSize,
                1));
        }

        public Task<TruckDetailsDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            TruckDetailsDto? truck = id == 1
                ? new TruckDetailsDto { TruckId = 1, TruckCode = "TRK-001", Status = TruckStatusDefinitions.InService }
                : null;
            return Task.FromResult(truck);
        }

        public Task<TruckFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TruckFilterOptions
            {
                Statuses = [TruckStatusDefinitions.InService],
                Models = ["Tombereau"]
            });
        }
    }

    private sealed class FakeDriverReadService : IDriverReadService
    {
        public DriverSearchCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<DriverListItemDto>> SearchAsync(
            DriverSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(PagedResult<DriverListItemDto>.Create(
                [new DriverListItemDto { DriverId = 1, DriverCode = "DRV-001", DisplayName = "Conducteur 01", Status = DriverStatusDefinitions.Active }],
                criteria.Page,
                criteria.PageSize,
                1));
        }

        public Task<DriverDetailsDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            DriverDetailsDto? driver = id == 1
                ? new DriverDetailsDto { DriverId = 1, DriverCode = "DRV-001", DisplayName = "Conducteur 01", Status = DriverStatusDefinitions.Active }
                : null;
            return Task.FromResult(driver);
        }

        public Task<DriverFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DriverFilterOptions
            {
                Statuses = [DriverStatusDefinitions.Active]
            });
        }
    }
}
