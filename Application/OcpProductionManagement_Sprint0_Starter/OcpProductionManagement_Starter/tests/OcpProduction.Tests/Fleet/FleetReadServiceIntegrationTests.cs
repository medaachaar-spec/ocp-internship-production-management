using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Drivers;
using OcpProduction.Infrastructure.Trucks;
using Xunit;

namespace OcpProduction.Tests.Fleet;

public sealed class FleetReadServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Truck_and_driver_reads_filter_page_and_preserve_database_state()
    {
        await using var context = CreateContext();
        var truckService = new TruckReadService(context);
        var driverService = new DriverReadService(context);
        var before = await GetFingerprintAsync(context);

        var trucks = await truckService.SearchAsync(new TruckSearchCriteria { PageSize = 10 });
        Assert.Equal(12, trucks.TotalItems);
        Assert.Equal(10, trucks.Items.Count);
        Assert.Equal(trucks.Items.OrderBy(item => item.TruckCode), trucks.Items);
        var truck = trucks.Items[0];
        var truckSearch = await truckService.SearchAsync(new TruckSearchCriteria { Search = truck.TruckCode });
        Assert.Contains(truckSearch.Items, item => item.TruckId == truck.TruckId);
        var truckStatus = await truckService.SearchAsync(new TruckSearchCriteria { Status = truck.Status });
        Assert.All(truckStatus.Items, item => Assert.Equal(truck.Status, item.Status));
        Assert.NotNull(await truckService.GetByIdAsync(truck.TruckId));
        Assert.Null(await truckService.GetByIdAsync(int.MaxValue));
        Assert.Contains(truck.Status, (await truckService.GetFilterOptionsAsync()).Statuses);

        var drivers = await driverService.SearchAsync(new DriverSearchCriteria { PageSize = 10 });
        Assert.Equal(20, drivers.TotalItems);
        Assert.Equal(10, drivers.Items.Count);
        Assert.Equal(drivers.Items.OrderBy(item => item.DriverCode), drivers.Items);
        var driver = drivers.Items[0];
        var driverSearch = await driverService.SearchAsync(new DriverSearchCriteria { Search = driver.DriverCode });
        Assert.Contains(driverSearch.Items, item => item.DriverId == driver.DriverId);
        var driverStatus = await driverService.SearchAsync(new DriverSearchCriteria { Status = driver.Status });
        Assert.All(driverStatus.Items, item => Assert.Equal(driver.Status, item.Status));
        Assert.NotNull(await driverService.GetByIdAsync(driver.DriverId));
        Assert.Null(await driverService.GetByIdAsync(int.MaxValue));

        var secondPage = await driverService.SearchAsync(new DriverSearchCriteria { Page = 2, PageSize = 10 });
        Assert.Equal(10, secondPage.Items.Count);
        Assert.DoesNotContain(secondPage.Items, candidate => drivers.Items.Any(first => first.DriverId == candidate.DriverId));

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

    private static async Task<FleetFingerprint> GetFingerprintAsync(OcpProductionDbContext context)
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
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Driver);
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new FleetFingerprint(reader.GetInt64(0), reader.GetInt32(1), reader.GetInt64(2), reader.GetInt32(3));
    }

    private sealed record FleetFingerprint(long TruckCount, int TruckChecksum, long DriverCount, int DriverChecksum);
}
