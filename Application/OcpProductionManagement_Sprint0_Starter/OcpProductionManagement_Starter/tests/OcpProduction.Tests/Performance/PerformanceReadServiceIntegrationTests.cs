using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Performance;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformanceReadServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Performance_views_filter_page_sort_detail_and_preserve_database_state()
    {
        await using var context = CreateContext();
        var service = new PerformanceReadService(context);
        var before = await GetFingerprintAsync(context);

        var trucks = await service.SearchTrucksAsync(new TruckPerformanceSearchCriteria { PageSize = 10 });
        Assert.Equal(12, trucks.TotalItems);
        Assert.Equal(10, trucks.Items.Count);
        var truck = trucks.Items[0];
        Assert.Contains(truck.TruckId, (await service.SearchTrucksAsync(new TruckPerformanceSearchCriteria { Search = truck.TruckCode })).Items.Select(item => item.TruckId));
        Assert.NotNull(await service.GetTruckAsync(truck.TruckId));
        Assert.Null(await service.GetTruckAsync(int.MaxValue));

        var drivers = await service.SearchDriversAsync(new DriverPerformanceSearchCriteria { PageSize = 10 });
        Assert.Equal(20, drivers.TotalItems);
        Assert.Equal(10, drivers.Items.Count);
        var driver = drivers.Items[0];
        Assert.Contains(driver.DriverId, (await service.SearchDriversAsync(new DriverPerformanceSearchCriteria { Search = driver.DriverCode })).Items.Select(item => item.DriverId));
        Assert.NotNull(await service.GetDriverAsync(driver.DriverId));
        Assert.Null(await service.GetDriverAsync(int.MaxValue));

        var filters = await service.GetFilterOptionsAsync();
        Assert.NotEmpty(filters.TruckStatuses);
        Assert.NotEmpty(filters.DriverStatuses);
        Assert.Equal(before, await GetFingerprintAsync(context));
    }

    private static OcpProductionDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>(optional: false).Build();
        var connectionString = configuration.GetConnectionString("OcpDatabase");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        return new OcpProductionDbContext(
            new DbContextOptionsBuilder<OcpProductionDbContext>().UseSqlServer(connectionString).Options);
    }

    private static async Task<PerformanceFingerprint> GetFingerprintAsync(OcpProductionDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.Truck),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Truck),
                (SELECT COUNT_BIG(*) FROM dbo.Driver),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Driver),
                (SELECT COUNT_BIG(*) FROM dbo.Trip),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Trip);
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new PerformanceFingerprint(
            reader.GetInt64(0),
            reader.GetInt32(1),
            reader.GetInt64(2),
            reader.GetInt32(3),
            reader.GetInt64(4),
            reader.GetInt32(5));
    }

    private sealed record PerformanceFingerprint(
        long TruckCount,
        int TruckChecksum,
        long DriverCount,
        int DriverChecksum,
        long TripCount,
        int TripChecksum);
}
