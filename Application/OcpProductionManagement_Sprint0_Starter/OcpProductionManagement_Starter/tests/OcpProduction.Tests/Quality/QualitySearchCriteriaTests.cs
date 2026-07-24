using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Trips.Models;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualitySearchCriteriaTests
{
    [Fact]
    public void Normalize_applies_safe_defaults_and_removes_invalid_values()
    {
        var normalized = new QualitySearchCriteria
        {
            Search = "   ",
            TripId = -1,
            TruckId = -1,
            P2O5Min = -0.001m,
            P2O5Max = -1m,
            Page = 0,
            PageSize = 999
        }.Normalize();

        Assert.Null(normalized.Search);
        Assert.Null(normalized.TripId);
        Assert.Null(normalized.TruckId);
        Assert.Null(normalized.P2O5Min);
        Assert.Null(normalized.P2O5Max);
        Assert.Equal(QualitySearchCriteria.DefaultPage, normalized.Page);
        Assert.Equal(QualitySearchCriteria.DefaultPageSize, normalized.PageSize);
    }

    [Fact]
    public void Normalize_preserves_the_verified_zero_trip_identifier()
    {
        var normalized = new QualitySearchCriteria { TripId = 0 }.Normalize();

        Assert.Equal(0, normalized.TripId);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Normalize_preserves_supported_page_sizes(int pageSize)
    {
        var normalized = new QualitySearchCriteria { PageSize = pageSize }.Normalize();

        Assert.Equal(pageSize, normalized.PageSize);
    }

    [Fact]
    public void Normalize_trims_search_and_safely_orders_measurement_bounds()
    {
        var normalized = new QualitySearchCriteria
        {
            Search = "  TRK-001   zone nord ",
            P2O5Min = 31.5m,
            P2O5Max = 29.5m
        }.Normalize();

        Assert.Equal("TRK-001 zone nord", normalized.Search);
        Assert.Equal(29.5m, normalized.P2O5Min);
        Assert.Equal(31.5m, normalized.P2O5Max);
    }

    [Fact]
    public void Normalize_preserves_inclusive_date_boundaries()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = DateOnly.MaxValue;

        var normalized = new QualitySearchCriteria
        {
            DateFrom = from,
            DateTo = to
        }.Normalize();

        Assert.Equal(from, normalized.DateFrom);
        Assert.Equal(to, normalized.DateTo);
    }

    [Fact]
    public void Paged_result_represents_zero_results_without_an_invalid_page()
    {
        var result = PagedResult<int>.Create([], page: 99, pageSize: 20, totalItems: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
