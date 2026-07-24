using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trucks.Models;
using Xunit;

namespace OcpProduction.Tests.Fleet;

public sealed class FleetCriteriaTests
{
    [Fact]
    public void Truck_criteria_normalizes_search_status_page_and_size()
    {
        var criteria = new TruckSearchCriteria
        {
            Search = "  TRK-001   classe  ",
            Status = "Unknown",
            Model = "  Tombereau  ",
            Page = 0,
            PageSize = 999
        }.Normalize();

        Assert.Equal("TRK-001 classe", criteria.Search);
        Assert.Null(criteria.Status);
        Assert.Equal("Tombereau", criteria.Model);
        Assert.Equal(1, criteria.Page);
        Assert.Equal(20, criteria.PageSize);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Truck_criteria_preserves_supported_page_sizes(int pageSize)
    {
        Assert.Equal(pageSize, new TruckSearchCriteria { PageSize = pageSize }.Normalize().PageSize);
    }

    [Fact]
    public void Driver_criteria_normalizes_search_status_page_and_size()
    {
        var criteria = new DriverSearchCriteria
        {
            Search = "  Conducteur   01  ",
            Status = DriverStatusDefinitions.Active,
            Page = -2,
            PageSize = 50
        }.Normalize();

        Assert.Equal("Conducteur 01", criteria.Search);
        Assert.Equal(DriverStatusDefinitions.Active, criteria.Status);
        Assert.Equal(1, criteria.Page);
        Assert.Equal(50, criteria.PageSize);
    }

    [Fact]
    public void Driver_criteria_rejects_an_unknown_actual_status()
    {
        Assert.Null(new DriverSearchCriteria { Status = "Unknown" }.Normalize().Status);
    }
}
