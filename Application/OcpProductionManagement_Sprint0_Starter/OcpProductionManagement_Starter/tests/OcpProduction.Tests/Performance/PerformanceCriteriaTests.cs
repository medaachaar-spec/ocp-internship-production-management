using OcpProduction.Application.Performance.Models;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformanceCriteriaTests
{
    [Fact]
    public void Truck_performance_criteria_normalizes_search_sort_page_and_size()
    {
        var criteria = new TruckPerformanceSearchCriteria
        {
            Search = "  TRK-001   modèle  ",
            Status = "  InService ",
            Sort = "Unexpected",
            Page = 0,
            PageSize = 999
        }.Normalize();

        Assert.Equal("TRK-001 modèle", criteria.Search);
        Assert.Equal("InService", criteria.Status);
        Assert.Equal(TruckPerformanceSort.TotalTripsDesc, criteria.Sort);
        Assert.Equal(1, criteria.Page);
        Assert.Equal(20, criteria.PageSize);
    }

    [Fact]
    public void Driver_performance_criteria_preserves_supported_sort_and_page_size()
    {
        var criteria = new DriverPerformanceSearchCriteria
        {
            Sort = DriverPerformanceSort.StopDurationDesc,
            Page = 2,
            PageSize = 50
        }.Normalize();

        Assert.Equal(DriverPerformanceSort.StopDurationDesc, criteria.Sort);
        Assert.Equal(2, criteria.Page);
        Assert.Equal(50, criteria.PageSize);
    }
}
