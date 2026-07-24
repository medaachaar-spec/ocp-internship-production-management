using OcpProduction.Application.Trips.Models;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripSearchCriteriaTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Normalize_replaces_invalid_page_with_page_one(int page)
    {
        var normalized = new TripSearchCriteria { Page = page }.Normalize();

        Assert.Equal(1, normalized.Page);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(1000)]
    public void Normalize_replaces_unsupported_page_size_with_twenty(int pageSize)
    {
        var normalized = new TripSearchCriteria { PageSize = pageSize }.Normalize();

        Assert.Equal(20, normalized.PageSize);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Normalize_keeps_supported_page_sizes(int pageSize)
    {
        var normalized = new TripSearchCriteria { PageSize = pageSize }.Normalize();

        Assert.Equal(pageSize, normalized.PageSize);
    }

    [Fact]
    public void Normalize_collapses_search_whitespace_and_trims_status()
    {
        var normalized = new TripSearchCriteria
        {
            Search = "  TRK-001   Point   de chargement  ",
            Status = "  Completed  "
        }.Normalize();

        Assert.Equal("TRK-001 Point de chargement", normalized.Search);
        Assert.Equal("Completed", normalized.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_converts_empty_search_to_null(string? search)
    {
        var normalized = new TripSearchCriteria { Search = search }.Normalize();

        Assert.Null(normalized.Search);
    }

    [Fact]
    public void Normalize_removes_non_positive_filter_identifiers()
    {
        var normalized = new TripSearchCriteria
        {
            TruckId = 0,
            DriverId = -1,
            WorkShiftId = 0
        }.Normalize();

        Assert.Null(normalized.TruckId);
        Assert.Null(normalized.DriverId);
        Assert.Null(normalized.WorkShiftId);
    }
}
