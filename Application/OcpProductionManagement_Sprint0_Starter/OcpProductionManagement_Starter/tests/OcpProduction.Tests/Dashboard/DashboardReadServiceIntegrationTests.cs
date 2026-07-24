using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Infrastructure.Dashboard;
using OcpProduction.Infrastructure.Data;
using Xunit;

namespace OcpProduction.Tests.Dashboard;

[Collection(DashboardDatabaseCollection.Name)]
public sealed class DashboardReadServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Corrected_KPIs_dates_and_remaining_linked_unlinked_stops_are_read_only()
    {
        await using var context = CreateContext();
        var service = new DashboardReadService(context);
        var before = await GetFingerprintAsync(context);

        var full = await service.GetDashboardAsync(new DashboardDateCriteria());
        Assert.Equal(801, full.TripCount);
        Assert.Equal(54533.80m, full.TotalTonnage);
        Assert.Equal(150, full.StopCount);
        Assert.Equal(416.40m, full.TotalStopDurationHours);
        Assert.Equal(120, full.QualityAnalysisCount);
        Assert.Equal(0, full.PendingValidationTripCount);
        AssertFullChartBaselines(full);
        AssertChartsReconcileWithKpis(full);

        var juneCriteria = new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30)
        };
        var june = await service.GetDashboardAsync(juneCriteria);
        Assert.Equal(800, june.TripCount);
        Assert.Equal(54473.80m, june.TotalTonnage);
        Assert.Equal(149, june.StopCount);
        Assert.Equal(415.50m, june.TotalStopDurationHours);
        Assert.Equal(120, june.QualityAnalysisCount);
        Assert.Equal(0, june.PendingValidationTripCount);
        AssertJuneChartBaselines(june);
        AssertChartsReconcileWithKpis(june);

        var throughJune = await service.GetDashboardAsync(new DashboardDateCriteria
        {
            EndDate = new DateOnly(2026, 6, 30)
        });
        Assert.Equal(june.TripCount, throughJune.TripCount);
        Assert.Equal(june.TotalTonnage, throughJune.TotalTonnage);
        Assert.Equal(june.StopCount, throughJune.StopCount);
        Assert.Equal(june.TotalStopDurationHours, throughJune.TotalStopDurationHours);
        Assert.Equal(june.QualityAnalysisCount, throughJune.QualityAnalysisCount);
        AssertEquivalentCharts(june.Charts, throughJune.Charts);

        var july = await service.GetDashboardAsync(new DashboardDateCriteria
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 20)
        });
        Assert.Equal(1, july.TripCount);
        Assert.Equal(60m, july.TotalTonnage);
        Assert.Equal(1, july.StopCount);
        Assert.Equal(0.90m, july.TotalStopDurationHours);
        Assert.Equal(0, july.QualityAnalysisCount);
        Assert.Equal(0, july.PendingValidationTripCount);
        Assert.Equal(full.TripCount - june.TripCount, july.TripCount);
        Assert.Equal(full.TotalTonnage - june.TotalTonnage, july.TotalTonnage);
        Assert.Equal(full.StopCount - june.StopCount, july.StopCount);
        Assert.Equal(
            full.TotalStopDurationHours - june.TotalStopDurationHours,
            july.TotalStopDurationHours);
        Assert.Equal(
            full.QualityAnalysisCount - june.QualityAnalysisCount,
            july.QualityAnalysisCount);
        AssertChartsReconcileWithKpis(july);
        Assert.Equal(
            54,
            july.Charts.StopDurationByReason.Sum(point => point.DurationMinutes));
        Assert.Equal(
            full.Charts.DailyTripsAndTonnage.Sum(point => point.TripCount),
            june.Charts.DailyTripsAndTonnage.Sum(point => point.TripCount) +
            july.Charts.DailyTripsAndTonnage.Sum(point => point.TripCount));
        Assert.Equal(
            full.Charts.DailyTripsAndTonnage.Sum(point => point.CompletedTonnage),
            june.Charts.DailyTripsAndTonnage.Sum(point => point.CompletedTonnage) +
            july.Charts.DailyTripsAndTonnage.Sum(point => point.CompletedTonnage));
        Assert.Equal(
            full.Charts.StopDurationByReason.Sum(point => point.DurationMinutes),
            june.Charts.StopDurationByReason.Sum(point => point.DurationMinutes) +
            july.Charts.StopDurationByReason.Sum(point => point.DurationMinutes));

        var emptyPeriod = await service.GetDashboardAsync(new DashboardDateCriteria
        {
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 1, 31)
        });
        Assert.Equal(0, emptyPeriod.TripCount);
        Assert.Equal(0m, emptyPeriod.TotalTonnage);
        Assert.Equal(0, emptyPeriod.StopCount);
        Assert.Equal(0m, emptyPeriod.TotalStopDurationHours);
        Assert.Equal(0, emptyPeriod.QualityAnalysisCount);
        Assert.Equal(0, emptyPeriod.PendingValidationTripCount);
        Assert.Empty(emptyPeriod.Charts.DailyTripsAndTonnage);
        Assert.Empty(emptyPeriod.Charts.TripsByStatus);
        Assert.Empty(emptyPeriod.Charts.StopDurationByReason);
        Assert.Empty(emptyPeriod.Charts.TruckActivity);
        Assert.Empty(emptyPeriod.Charts.P2O5Trend);

        var linkedStops = await context.vw_StopAnalyses.AsNoTracking()
            .Where(stop => stop.TripID != null)
            .Select(stop => stop.TruckStopID)
            .Distinct()
            .LongCountAsync();
        var unlinkedStops = await context.vw_StopAnalyses.AsNoTracking()
            .Where(stop => stop.TripID == null)
            .Select(stop => stop.TruckStopID)
            .Distinct()
            .LongCountAsync();
        Assert.Equal(120, linkedStops);
        Assert.Equal(30, unlinkedStops);
        Assert.Equal(full.StopCount, linkedStops + unlinkedStops);
        Assert.False(await context.vw_StopAnalyses.AsNoTracking()
            .AnyAsync(stop => stop.TruckStopID == 153));

        var remainingStopDurationMinutes = await context.vw_StopAnalyses.AsNoTracking()
            .SumAsync(stop => Convert.ToInt64(stop.StopDurationMinutes ?? 0));
        Assert.Equal(24984, remainingStopDurationMinutes);
        Assert.Equal(
            remainingStopDurationMinutes,
            full.Charts.StopDurationByReason.Sum(point => point.DurationMinutes));

        var linkedQualityResults = await context.vw_QualityAnalyses.AsNoTracking()
            .Where(result => result.TripID != null)
            .Select(result => result.QualityResultID)
            .Distinct()
            .LongCountAsync();
        var unlinkedQualityResults = await context.vw_QualityAnalyses.AsNoTracking()
            .Where(result => result.TripID == null)
            .Select(result => result.QualityResultID)
            .Distinct()
            .LongCountAsync();
        Assert.True(unlinkedQualityResults > 0);
        Assert.Equal(
            full.QualityAnalysisCount,
            linkedQualityResults + unlinkedQualityResults);

        Assert.Equal(before, await GetFingerprintAsync(context));
    }

    private static void AssertFullChartBaselines(DashboardDto dashboard)
    {
        Assert.Equal(31, dashboard.Charts.DailyTripsAndTonnage.Count);
        Assert.Equal(new DateOnly(2026, 6, 1), dashboard.Charts.DailyTripsAndTonnage[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 20), dashboard.Charts.DailyTripsAndTonnage[^1].Date);

        Assert.Collection(
            dashboard.Charts.TripsByStatus,
            point =>
            {
                Assert.Equal("Completed", point.Status);
                Assert.Equal(783, point.TripCount);
            },
            point =>
            {
                Assert.Equal("Incomplete", point.Status);
                Assert.Equal(13, point.TripCount);
            },
            point =>
            {
                Assert.Equal("InProgress", point.Status);
                Assert.Equal(5, point.TripCount);
            });

        Assert.Equal(8, dashboard.Charts.StopDurationByReason.Count);
        var longestStopReason = dashboard.Charts.StopDurationByReason[0];
        Assert.Equal(3, longestStopReason.StopReasonId);
        Assert.Equal("Panne hydraulique", longestStopReason.StopReasonName);
        Assert.Equal(15, longestStopReason.StopCount);
        Assert.Equal(5641, longestStopReason.DurationMinutes);
        Assert.Equal(5641m / 60m, longestStopReason.DurationHours);

        Assert.Equal(12, dashboard.Charts.TruckActivity.Count);
        Assert.Equal("TRK-012", dashboard.Charts.TruckActivity[0].TruckCode);
        Assert.Equal(81, dashboard.Charts.TruckActivity[0].TripCount);

        Assert.Equal(30, dashboard.Charts.P2O5Trend.Count);
        Assert.Equal(new DateOnly(2026, 6, 1), dashboard.Charts.P2O5Trend[0].Date);
        Assert.Equal(29.5475m, dashboard.Charts.P2O5Trend[0].AverageP2O5);
        Assert.Equal(4, dashboard.Charts.P2O5Trend[0].MeasurementCount);
        Assert.Equal(new DateOnly(2026, 6, 30), dashboard.Charts.P2O5Trend[^1].Date);
        Assert.Equal(29.9625m, dashboard.Charts.P2O5Trend[^1].AverageP2O5);
    }

    private static void AssertJuneChartBaselines(DashboardDto dashboard)
    {
        Assert.Equal(30, dashboard.Charts.DailyTripsAndTonnage.Count);
        Assert.Equal(new DateOnly(2026, 6, 1), dashboard.Charts.DailyTripsAndTonnage[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 30), dashboard.Charts.DailyTripsAndTonnage[^1].Date);

        Assert.Collection(
            dashboard.Charts.TripsByStatus,
            point =>
            {
                Assert.Equal("Completed", point.Status);
                Assert.Equal(782, point.TripCount);
            },
            point =>
            {
                Assert.Equal("Incomplete", point.Status);
                Assert.Equal(13, point.TripCount);
            },
            point =>
            {
                Assert.Equal("InProgress", point.Status);
                Assert.Equal(5, point.TripCount);
            });

        Assert.Equal(8, dashboard.Charts.StopDurationByReason.Count);
        Assert.Equal(3, dashboard.Charts.StopDurationByReason[0].StopReasonId);
        Assert.Equal(5641, dashboard.Charts.StopDurationByReason[0].DurationMinutes);

        Assert.Equal(12, dashboard.Charts.TruckActivity.Count);
        Assert.Equal("TRK-011", dashboard.Charts.TruckActivity[0].TruckCode);
        Assert.Equal(80, dashboard.Charts.TruckActivity[0].TripCount);
        Assert.Equal("TRK-012", dashboard.Charts.TruckActivity[1].TruckCode);
        Assert.Equal(80, dashboard.Charts.TruckActivity[1].TripCount);

        Assert.Equal(30, dashboard.Charts.P2O5Trend.Count);
    }

    private static void AssertChartsReconcileWithKpis(DashboardDto dashboard)
    {
        Assert.Equal(
            dashboard.TripCount,
            dashboard.Charts.DailyTripsAndTonnage.Sum(point => point.TripCount));
        Assert.Equal(
            dashboard.TotalTonnage,
            dashboard.Charts.DailyTripsAndTonnage.Sum(point => point.CompletedTonnage));
        Assert.Equal(
            dashboard.TripCount,
            dashboard.Charts.TripsByStatus.Sum(point => point.TripCount));
        Assert.Equal(
            dashboard.TripCount,
            dashboard.Charts.TruckActivity.Sum(point => point.TripCount));
        Assert.Equal(
            dashboard.StopCount,
            dashboard.Charts.StopDurationByReason.Sum(point => point.StopCount));
        Assert.Equal(
            dashboard.TotalStopDurationHours,
            dashboard.Charts.StopDurationByReason.Sum(point => point.DurationHours));
        Assert.Equal(
            dashboard.QualityAnalysisCount,
            dashboard.Charts.P2O5Trend.Sum(point => point.MeasurementCount));

        Assert.Equal(
            dashboard.Charts.DailyTripsAndTonnage.Select(point => point.Date),
            dashboard.Charts.DailyTripsAndTonnage
                .OrderBy(point => point.Date)
                .Select(point => point.Date));
        Assert.Equal(
            dashboard.Charts.TripsByStatus.Select(point => point.Status),
            dashboard.Charts.TripsByStatus
                .OrderBy(point => point.Status, StringComparer.OrdinalIgnoreCase)
                .Select(point => point.Status));
        Assert.Equal(
            dashboard.Charts.StopDurationByReason.Select(
                point => (point.DurationMinutes, point.StopReasonName, point.StopReasonId)),
            dashboard.Charts.StopDurationByReason
                .OrderByDescending(point => point.DurationMinutes)
                .ThenBy(point => point.StopReasonName, StringComparer.Ordinal)
                .ThenBy(point => point.StopReasonId)
                .Select(point =>
                    (point.DurationMinutes, point.StopReasonName, point.StopReasonId)));
        Assert.Equal(
            dashboard.Charts.TruckActivity.Select(
                point => (point.TripCount, point.TruckCode, point.TruckId)),
            dashboard.Charts.TruckActivity
                .OrderByDescending(point => point.TripCount)
                .ThenBy(point => point.TruckCode, StringComparer.Ordinal)
                .ThenBy(point => point.TruckId)
                .Select(point => (point.TripCount, point.TruckCode, point.TruckId)));
        Assert.Equal(
            dashboard.Charts.P2O5Trend.Select(point => point.Date),
            dashboard.Charts.P2O5Trend
                .OrderBy(point => point.Date)
                .Select(point => point.Date));
    }

    private static void AssertEquivalentCharts(
        DashboardChartDataDto expected,
        DashboardChartDataDto actual)
    {
        Assert.Equal(
            expected.DailyTripsAndTonnage.Select(
                point => (point.Date, point.TripCount, point.CompletedTonnage)),
            actual.DailyTripsAndTonnage.Select(
                point => (point.Date, point.TripCount, point.CompletedTonnage)));
        Assert.Equal(
            expected.TripsByStatus.Select(point => (point.Status, point.TripCount)),
            actual.TripsByStatus.Select(point => (point.Status, point.TripCount)));
        Assert.Equal(
            expected.StopDurationByReason.Select(point =>
                (point.StopReasonId, point.StopReasonName, point.StopCount, point.DurationMinutes)),
            actual.StopDurationByReason.Select(point =>
                (point.StopReasonId, point.StopReasonName, point.StopCount, point.DurationMinutes)));
        Assert.Equal(
            expected.TruckActivity.Select(point =>
                (point.TruckId, point.TruckCode, point.TripCount)),
            actual.TruckActivity.Select(point =>
                (point.TruckId, point.TruckCode, point.TripCount)));
        Assert.Equal(
            expected.P2O5Trend.Select(point =>
                (point.Date, point.AverageP2O5, point.MeasurementCount)),
            actual.P2O5Trend.Select(point =>
                (point.Date, point.AverageP2O5, point.MeasurementCount)));
    }

    private static OcpProductionDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: false)
            .Build();
        var connectionString = configuration.GetConnectionString("OcpDatabase");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        return new OcpProductionDbContext(
            new DbContextOptionsBuilder<OcpProductionDbContext>()
                .UseSqlServer(connectionString)
                .Options);
    }

    private static async Task<DashboardFingerprint> GetFingerprintAsync(
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
                (SELECT COUNT_BIG(*) FROM dbo.Trip),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Trip),
                (SELECT COUNT_BIG(*) FROM dbo.TruckStop),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.TruckStop),
                (SELECT COUNT_BIG(*) FROM dbo.QualityResult),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.QualityResult);
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new DashboardFingerprint(
            reader.GetInt64(0),
            reader.GetInt32(1),
            reader.GetInt64(2),
            reader.GetInt32(3),
            reader.GetInt64(4),
            reader.GetInt32(5));
    }

    private sealed record DashboardFingerprint(
        long TripCount,
        int TripChecksum,
        long StopCount,
        int StopChecksum,
        long QualityCount,
        int QualityChecksum);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DashboardDatabaseCollection
{
    public const string Name = "Dashboard database integration";
}
