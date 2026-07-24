using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Quality;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityReadServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Read_service_filters_pages_and_preserves_database_fingerprint()
    {
        await using var context = CreateContext();
        var service = new QualityReadService(context);
        var before = await GetFingerprintAsync(context);

        var firstPage = await service.SearchAsync(new QualitySearchCriteria
        {
            Page = 1,
            PageSize = 10
        });

        Assert.NotEmpty(firstPage.Items);
        Assert.Equal(before.Count, firstPage.TotalItems);
        Assert.Equal(10, firstPage.PageSize);
        Assert.Equal(
            firstPage.Items
                .OrderByDescending(result => result.AnalysisDate)
                .ThenByDescending(result => result.QualityResultId),
            firstPage.Items);

        var first = firstPage.Items[0];
        var details = await service.GetByIdAsync(first.QualityResultId);
        Assert.NotNull(details);
        Assert.Equal(first.QualityResultId, details.QualityResultId);
        Assert.Equal(first.TripId, details.TripId);
        Assert.Equal(first.P2O5Value, details.QualityValue);

        var date = DateOnly.FromDateTime(first.AnalysisDate);
        var dateResults = await service.SearchAsync(new QualitySearchCriteria
        {
            DateFrom = date,
            DateTo = date,
            PageSize = 50
        });
        Assert.NotEmpty(dateResults.Items);
        Assert.All(dateResults.Items, result =>
            Assert.Equal(date, DateOnly.FromDateTime(result.AnalysisDate)));

        var maximumEndDate = await service.SearchAsync(new QualitySearchCriteria
        {
            DateTo = DateOnly.MaxValue,
            PageSize = 10
        });
        Assert.Equal(firstPage.TotalItems, maximumEndDate.TotalItems);

        if (first.TripId is { } tripId)
        {
            var tripResults = await service.SearchAsync(new QualitySearchCriteria
            {
                TripId = tripId,
                PageSize = 50
            });
            Assert.NotEmpty(tripResults.Items);
            Assert.All(tripResults.Items, result => Assert.Equal(tripId, result.TripId));
        }

        if (first.TruckId is { } truckId)
        {
            var truckResults = await service.SearchAsync(new QualitySearchCriteria
            {
                TruckId = truckId,
                PageSize = 50
            });
            Assert.NotEmpty(truckResults.Items);
            Assert.All(truckResults.Items, result => Assert.Equal(truckId, result.TruckId));
        }

        if (first.P2O5Value is { } measurement)
        {
            var rangeResults = await service.SearchAsync(new QualitySearchCriteria
            {
                P2O5Min = measurement,
                P2O5Max = measurement,
                PageSize = 50
            });
            Assert.NotEmpty(rangeResults.Items);
            Assert.All(rangeResults.Items, result => Assert.Equal(measurement, result.P2O5Value));
        }

        var statusResults = await service.SearchAsync(new QualitySearchCriteria
        {
            Status = first.Status,
            PageSize = 50
        });
        Assert.NotEmpty(statusResults.Items);
        Assert.All(statusResults.Items, result => Assert.Equal(first.Status, result.Status));

        var idSearch = await service.SearchAsync(new QualitySearchCriteria
        {
            Search = first.QualityResultId.ToString(),
            PageSize = 10
        });
        Assert.Contains(idSearch.Items, result => result.QualityResultId == first.QualityResultId);

        if (firstPage.TotalPages > 1)
        {
            var secondPage = await service.SearchAsync(new QualitySearchCriteria
            {
                Page = 2,
                PageSize = 10
            });
            Assert.DoesNotContain(secondPage.Items, candidate =>
                firstPage.Items.Any(firstCandidate =>
                    firstCandidate.QualityResultId == candidate.QualityResultId));
        }

        var options = await service.GetFilterOptionsAsync();
        Assert.Contains(first.Status, options.Statuses);

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

    private static async Task<QualityFingerprint> GetFingerprintAsync(
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
                    QualityResultID,
                    AnalysisDate,
                    TripID,
                    LoadingPointID,
                    QualityValue,
                    QualityUnit,
                    TargetValue,
                    Status,
                    Comments,
                    CreatedAt))
            FROM dbo.QualityResult;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new QualityFingerprint(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    private sealed record QualityFingerprint(long Count, int? Checksum);
}
