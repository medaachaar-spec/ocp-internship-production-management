using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Trips.Models;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopSearchCriteriaTests
{
    [Fact]
    public void Normalize_applies_safe_defaults_and_normalizes_search()
    {
        var normalized = new StopSearchCriteria
        {
            Search = "  TRK-001   panne  ",
            Page = -2,
            PageSize = 999,
            TruckId = 0,
            StopReasonId = -1,
            TripId = 0
        }.Normalize();

        Assert.Equal("TRK-001 panne", normalized.Search);
        Assert.Equal(1, normalized.Page);
        Assert.Equal(20, normalized.PageSize);
        Assert.Null(normalized.TruckId);
        Assert.Null(normalized.StopReasonId);
        Assert.Null(normalized.TripId);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Normalize_preserves_supported_page_sizes(int pageSize)
    {
        var normalized = new StopSearchCriteria { PageSize = pageSize }.Normalize();

        Assert.Equal(pageSize, normalized.PageSize);
    }

    [Fact]
    public void Normalize_accepts_known_state_and_status_case_insensitively()
    {
        var normalized = new StopSearchCriteria
        {
            State = " open ",
            ValidationStatus = " validated "
        }.Normalize();

        Assert.Equal(StopStates.Open, normalized.State);
        Assert.Equal(StopValidationStatuses.Validated, normalized.ValidationStatus);
    }

    [Fact]
    public void Normalize_ignores_unknown_state_and_status_and_empty_search()
    {
        var normalized = new StopSearchCriteria
        {
            Search = "   ",
            State = "Paused",
            ValidationStatus = "Unknown"
        }.Normalize();

        Assert.Null(normalized.Search);
        Assert.Null(normalized.State);
        Assert.Null(normalized.ValidationStatus);
    }

    [Fact]
    public void Normalize_preserves_inclusive_date_range()
    {
        var date = new DateOnly(2026, 6, 15);

        var normalized = new StopSearchCriteria { DateFrom = date, DateTo = date }.Normalize();

        Assert.Equal(date, normalized.DateFrom);
        Assert.Equal(date, normalized.DateTo);
    }

    [Fact]
    public void Empty_stop_result_has_safe_zero_page_pagination()
    {
        var result = PagedResult<string>.Create([], page: 99, pageSize: 20, totalItems: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
