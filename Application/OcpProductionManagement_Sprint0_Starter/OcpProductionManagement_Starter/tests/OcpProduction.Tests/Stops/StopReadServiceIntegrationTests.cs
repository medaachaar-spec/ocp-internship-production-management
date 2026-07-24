using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopReadServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Read_service_filters_pages_and_preserves_database_fingerprint()
    {
        await using var context = CreateContext();
        var service = new StopReadService(context);
        var before = await GetFingerprintAsync(context);

        var firstPage = await service.SearchAsync(new StopSearchCriteria
        {
            Page = 1,
            PageSize = 10
        });

        Assert.NotEmpty(firstPage.Items);
        Assert.True(firstPage.TotalItems >= firstPage.Items.Count);
        Assert.Equal(10, firstPage.PageSize);
        Assert.Equal(
            firstPage.Items.OrderByDescending(stop => stop.StartTime).ThenByDescending(stop => stop.StopId),
            firstPage.Items);

        var first = firstPage.Items[0];
        var details = await service.GetByIdAsync(first.StopId);
        Assert.NotNull(details);
        Assert.Equal(first.StopId, details.StopId);
        Assert.Equal(first.TruckId, details.TruckId);
        Assert.Equal(first.StopReasonId, details.StopReasonId);

        var truckFilter = await service.SearchAsync(new StopSearchCriteria
        {
            TruckId = first.TruckId,
            PageSize = 50
        });
        Assert.NotEmpty(truckFilter.Items);
        Assert.All(truckFilter.Items, stop => Assert.Equal(first.TruckId, stop.TruckId));

        var reasonFilter = await service.SearchAsync(new StopSearchCriteria
        {
            StopReasonId = first.StopReasonId,
            PageSize = 50
        });
        Assert.NotEmpty(reasonFilter.Items);
        Assert.All(reasonFilter.Items, stop => Assert.Equal(first.StopReasonId, stop.StopReasonId));

        var stateFilter = await service.SearchAsync(new StopSearchCriteria
        {
            State = first.State,
            PageSize = 50
        });
        Assert.NotEmpty(stateFilter.Items);
        Assert.All(stateFilter.Items, stop => Assert.Equal(first.State, stop.State));

        var statusFilter = await service.SearchAsync(new StopSearchCriteria
        {
            ValidationStatus = first.ValidationStatus,
            PageSize = 50
        });
        Assert.NotEmpty(statusFilter.Items);
        Assert.All(statusFilter.Items, stop =>
            Assert.Equal(first.ValidationStatus, stop.ValidationStatus));

        var date = DateOnly.FromDateTime(first.StartTime);
        var sameDay = await service.SearchAsync(new StopSearchCriteria
        {
            DateFrom = date,
            DateTo = date,
            PageSize = 50
        });
        Assert.NotEmpty(sameDay.Items);
        Assert.All(sameDay.Items, stop =>
            Assert.Equal(date, DateOnly.FromDateTime(stop.StartTime)));

        var maximumEndDate = await service.SearchAsync(new StopSearchCriteria
        {
            DateTo = DateOnly.MaxValue,
            PageSize = 10
        });
        Assert.Equal(firstPage.TotalItems, maximumEndDate.TotalItems);

        var search = await service.SearchAsync(new StopSearchCriteria
        {
            Search = first.TruckCode,
            PageSize = 50
        });
        Assert.NotEmpty(search.Items);
        Assert.All(search.Items, stop => Assert.Equal(first.TruckCode, stop.TruckCode));

        if (first.TripId is { } tripId)
        {
            var tripFilter = await service.SearchAsync(new StopSearchCriteria
            {
                TripId = tripId,
                PageSize = 50
            });
            Assert.NotEmpty(tripFilter.Items);
            Assert.All(tripFilter.Items, stop => Assert.Equal(tripId, stop.TripId));
        }

        if (firstPage.TotalPages > 1)
        {
            var secondPage = await service.SearchAsync(new StopSearchCriteria
            {
                Page = 2,
                PageSize = 10
            });
            Assert.DoesNotContain(secondPage.Items, candidate =>
                firstPage.Items.Any(firstCandidate => firstCandidate.StopId == candidate.StopId));
        }

        var options = await service.GetFilterOptionsAsync();
        Assert.Contains(options.Trucks, option => option.Value == first.TruckId);
        Assert.Contains(options.StopReasons, option => option.Value == first.StopReasonId);
        Assert.Contains(first.ValidationStatus, options.ValidationStatuses);

        var missing = await service.GetByIdAsync(long.MaxValue);
        Assert.Null(missing);

        var after = await GetFingerprintAsync(context);
        Assert.Equal(before, after);
    }

    private static OcpProductionDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: false)
            .Build();
        var connectionString = configuration.GetConnectionString("OcpDatabase");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "ConnectionStrings:OcpDatabase must exist in .NET User Secrets for integration tests.");

        var options = new DbContextOptionsBuilder<OcpProductionDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new OcpProductionDbContext(options);
    }

    private static async Task<StopFingerprint> GetFingerprintAsync(
        OcpProductionDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COUNT_BIG(*),
                CHECKSUM_AGG(BINARY_CHECKSUM(
                    TruckStopID,
                    TruckID,
                    DriverID,
                    ShiftID,
                    TripID,
                    StopReasonID,
                    StartTime,
                    EndTime,
                    DurationMinutes,
                    ValidationStatus,
                    Comments,
                    CreatedAt))
            FROM dbo.TruckStop;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new StopFingerprint(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    private sealed record StopFingerprint(long Count, int? Checksum);
}
