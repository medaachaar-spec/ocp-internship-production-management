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

        var truckReport = await service.GetTruckReportAsync(
            new TruckPerformanceSearchCriteria { PageSize = 10 });
        Assert.Equal(12, truckReport.FilteredItems.Count);
        Assert.Equal(12, truckReport.Summary.DisplayedTruckCount);
        Assert.Equal(801, truckReport.Summary.TripCount);
        Assert.Equal(54533.80m, truckReport.Summary.CompletedTonnage);
        Assert.Equal(150, truckReport.Summary.StopCount);
        Assert.Equal(24984, truckReport.Summary.TotalStopDurationMinutes);
        Assert.Equal(10, truckReport.TableRows.Items.Count);
        Assert.Equal(12, truckReport.Charts.TripsByTruck.Count);
        Assert.Equal(
            truckReport.Summary.TripCount,
            truckReport.Charts.TripsByTruck.Sum(point => point.Value));
        Assert.Equal(
            truckReport.Summary.CompletedTonnage,
            truckReport.Charts.TonnageByTruck.Sum(point => point.Value));
        Assert.Equal(
            truckReport.Summary.StopCount,
            truckReport.Charts.StopsByTruck.Sum(point => point.Value));
        Assert.All(
            truckReport.Charts.P2O5ByTruck,
            point => Assert.True(point.Value >= 0));

        var filteredTruckReport = await service.GetTruckReportAsync(
            new TruckPerformanceSearchCriteria
            {
                Search = truck.TruckCode,
                PageSize = 10
            });
        Assert.Single(filteredTruckReport.FilteredItems);
        Assert.Single(filteredTruckReport.Charts.TripsByTruck);
        Assert.Equal(
            filteredTruckReport.FilteredItems.Sum(item => item.TotalTrips),
            filteredTruckReport.Summary.TripCount);
        Assert.Equal(
            filteredTruckReport.FilteredItems.Sum(item => item.CompletedTonnage),
            filteredTruckReport.Summary.CompletedTonnage);
        Assert.Equal(
            filteredTruckReport.FilteredItems.Sum(item => item.TotalStopCount ?? 0),
            filteredTruckReport.Summary.StopCount);

        var driverReport = await service.GetDriverReportAsync(
            new DriverPerformanceSearchCriteria { PageSize = 10 });
        Assert.Equal(20, driverReport.FilteredItems.Count);
        Assert.Equal(20, driverReport.Summary.DisplayedDriverCount);
        Assert.Equal(801, driverReport.Summary.TripCount);
        Assert.Equal(783, driverReport.Summary.CompletedTripCount);
        Assert.Equal(54533.80m, driverReport.Summary.CompletedTonnage);
        Assert.Equal(120, driverReport.Summary.LinkedStopCount);
        Assert.Equal(10, driverReport.TableRows.Items.Count);
        Assert.Equal(20, driverReport.Charts.TripsByDriver.Count);
        Assert.Equal(20, driverReport.Charts.TonnageByDriver.Count);
        Assert.Equal(20, driverReport.Charts.LinkedStopsByDriver.Count);
        Assert.Equal(
            driverReport.Summary.TripCount,
            driverReport.Charts.TripsByDriver.Sum(point => point.TotalTrips));
        Assert.Equal(
            driverReport.Summary.CompletedTripCount,
            driverReport.Charts.TripsByDriver.Sum(point => point.CompletedTrips));
        Assert.Equal(
            driverReport.Summary.CompletedTonnage,
            driverReport.Charts.TonnageByDriver.Sum(point => point.Value));
        Assert.Equal(
            driverReport.Summary.LinkedStopCount,
            driverReport.Charts.LinkedStopsByDriver.Sum(point => point.Value));

        var filteredDriverReport = await service.GetDriverReportAsync(
            new DriverPerformanceSearchCriteria
            {
                Search = driver.DriverCode,
                PageSize = 10
            });
        Assert.Single(filteredDriverReport.FilteredItems);
        Assert.Single(filteredDriverReport.Charts.TripsByDriver);
        Assert.Equal(
            filteredDriverReport.FilteredItems.Sum(item => item.TotalTrips),
            filteredDriverReport.Summary.TripCount);
        Assert.Equal(
            filteredDriverReport.FilteredItems.Sum(item => item.CompletedTonnage),
            filteredDriverReport.Summary.CompletedTonnage);
        Assert.Equal(
            filteredDriverReport.FilteredItems.Sum(item => item.TotalLinkedStopCount ?? 0),
            filteredDriverReport.Summary.LinkedStopCount);

        var filters = await service.GetFilterOptionsAsync();
        Assert.NotEmpty(filters.TruckStatuses);
        Assert.NotEmpty(filters.DriverStatuses);
        var truckStatusReport = await service.GetTruckReportAsync(
            new TruckPerformanceSearchCriteria
            {
                Status = filters.TruckStatuses[0],
                PageSize = 10
            });
        Assert.NotEmpty(truckStatusReport.FilteredItems);
        Assert.All(
            truckStatusReport.FilteredItems,
            item => Assert.Equal(filters.TruckStatuses[0], item.TruckStatus));
        var driverStatusReport = await service.GetDriverReportAsync(
            new DriverPerformanceSearchCriteria
            {
                Status = filters.DriverStatuses[0],
                PageSize = 10
            });
        Assert.NotEmpty(driverStatusReport.FilteredItems);
        Assert.All(
            driverStatusReport.FilteredItems,
            item => Assert.Equal(filters.DriverStatuses[0], item.DriverStatus));
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
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Trip),
                (SELECT COUNT_BIG(*) FROM dbo.TruckStop),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.TruckStop),
                (SELECT COUNT_BIG(*) FROM dbo.QualityResult),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.QualityResult),
                CHECKSUM(OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_TruckPerformance'))),
                CHECKSUM(OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_DriverPerformance'))),
                CHECKSUM(OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_TripSummary'))),
                CHECKSUM(OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_StopAnalysis'))),
                CHECKSUM(OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_QualityAnalysis')));
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new PerformanceFingerprint(
            reader.GetInt64(0),
            reader.GetInt32(1),
            reader.GetInt64(2),
            reader.GetInt32(3),
            reader.GetInt64(4),
            reader.GetInt32(5),
            reader.GetInt64(6),
            reader.GetInt32(7),
            reader.GetInt64(8),
            reader.GetInt32(9),
            reader.GetInt32(10),
            reader.GetInt32(11),
            reader.GetInt32(12),
            reader.GetInt32(13),
            reader.GetInt32(14));
    }

    private sealed record PerformanceFingerprint(
        long TruckCount,
        int TruckChecksum,
        long DriverCount,
        int DriverChecksum,
        long TripCount,
        int TripChecksum,
        long TruckStopCount,
        int TruckStopChecksum,
        long QualityResultCount,
        int QualityResultChecksum,
        int TruckPerformanceViewChecksum,
        int DriverPerformanceViewChecksum,
        int TripSummaryViewChecksum,
        int StopAnalysisViewChecksum,
        int QualityAnalysisViewChecksum);
}
