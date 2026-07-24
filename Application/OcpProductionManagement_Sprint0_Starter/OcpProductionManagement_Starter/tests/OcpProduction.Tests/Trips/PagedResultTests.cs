using OcpProduction.Application.Trips.Models;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class PagedResultTests
{
    [Fact]
    public void Create_calculates_total_pages_and_navigation_flags()
    {
        var result = PagedResult<int>.Create([21, 22], page: 2, pageSize: 20, totalItems: 41);

        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Create_handles_zero_results_safely()
    {
        var result = PagedResult<int>.Create([], page: 25, pageSize: 20, totalItems: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Create_clamps_page_to_last_available_page()
    {
        var result = PagedResult<int>.Create([], page: 99, pageSize: 10, totalItems: 21);

        Assert.Equal(3, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
