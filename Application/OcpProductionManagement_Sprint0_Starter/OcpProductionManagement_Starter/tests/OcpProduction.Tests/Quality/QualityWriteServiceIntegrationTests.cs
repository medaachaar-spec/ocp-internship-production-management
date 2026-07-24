using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OcpProduction.Application.Quality.Commands;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Quality;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityWriteServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Create_and_update_are_validated_and_fully_rolled_back()
    {
        await using var context = CreateContext();
        var before = await CaptureSnapshotAsync(context);
        var marker = $"CODEX-QUALITY-ROLLBACK-{Guid.NewGuid():N}";
        long? createdId = null;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var source = await context.Trips
                .AsNoTracking()
                .OrderBy(trip => trip.TripID)
                .Select(trip => new { trip.TripID, trip.LoadingPointID })
                .FirstAsync();
            var alternateLoadingPointId = await context.LoadingPoints
                .AsNoTracking()
                .Where(point => point.LoadingPointID != source.LoadingPointID)
                .Select(point => (int?)point.LoadingPointID)
                .FirstOrDefaultAsync();
            Assert.NotNull(alternateLoadingPointId);

            var service = new QualityWriteService(
                context,
                NullLogger<QualityWriteService>.Instance);
            var analysisDate = new DateTime(2030, 1, 2, 3, 4, 5);

            var createResult = await service.CreateAsync(new CreateQualityResultCommand
            {
                AnalysisDate = analysisDate,
                TripId = source.TripID,
                LoadingPointId = source.LoadingPointID,
                QualityValue = 30.125m,
                Comments = $"  {marker}  "
            });

            Assert.True(
                createResult.Success,
                $"Create failed with {createResult.ErrorCode}: {createResult.ErrorMessage ?? string.Join(" | ", createResult.ValidationErrors.Values)}");
            createdId = Assert.IsType<long>(createResult.QualityResultId);
            Assert.True(createdId > 0);

            var stored = await context.QualityResults
                .AsNoTracking()
                .SingleAsync(result => result.QualityResultID == createdId);
            Assert.Equal(analysisDate, stored.AnalysisDate);
            Assert.Equal(source.TripID, stored.TripID);
            Assert.Equal(source.LoadingPointID, stored.LoadingPointID);
            Assert.Equal(30.125m, stored.QualityValue);
            Assert.Equal("P2O5Percent", stored.QualityUnit);
            Assert.Equal(QualityStatusDefinitions.Pending, stored.Status);
            Assert.Null(stored.TargetValue);
            Assert.Equal(marker, stored.Comments);
            Assert.NotEqual(default, stored.CreatedAt);

            var protectedValues = new
            {
                stored.QualityUnit,
                stored.TargetValue,
                stored.Status,
                stored.CreatedAt
            };

            var updateResult = await service.UpdateAsync(new UpdateQualityResultCommand
            {
                QualityResultId = createdId.Value,
                AnalysisDate = analysisDate.AddHours(1),
                TripId = source.TripID,
                LoadingPointId = source.LoadingPointID,
                QualityValue = 30.250m,
                Comments = marker + "-UPDATED"
            });

            Assert.True(updateResult.Success);
            var updated = await context.QualityResults
                .AsNoTracking()
                .SingleAsync(result => result.QualityResultID == createdId);
            Assert.Equal(analysisDate.AddHours(1), updated.AnalysisDate);
            Assert.Equal(30.250m, updated.QualityValue);
            Assert.Equal(marker + "-UPDATED", updated.Comments);
            Assert.Equal(protectedValues.QualityUnit, updated.QualityUnit);
            Assert.Equal(protectedValues.TargetValue, updated.TargetValue);
            Assert.Equal(protectedValues.Status, updated.Status);
            Assert.Equal(protectedValues.CreatedAt, updated.CreatedAt);

            var missingReference = await service.CreateAsync(new CreateQualityResultCommand
            {
                AnalysisDate = analysisDate,
                TripId = long.MaxValue,
                QualityValue = 30.125m
            });
            Assert.Equal(QualityWriteErrorCode.Validation, missingReference.ErrorCode);
            Assert.Contains(nameof(CreateQualityResultCommand.TripId), missingReference.ValidationErrors.Keys);

            var incompatibleSource = await service.CreateAsync(new CreateQualityResultCommand
            {
                AnalysisDate = analysisDate,
                TripId = source.TripID,
                LoadingPointId = alternateLoadingPointId,
                QualityValue = 30.125m
            });
            Assert.Equal(QualityWriteErrorCode.Validation, incompatibleSource.ErrorCode);
            Assert.Contains(
                nameof(CreateQualityResultCommand.LoadingPointId),
                incompatibleSource.ValidationErrors.Keys);

            var excessiveScale = await service.CreateAsync(new CreateQualityResultCommand
            {
                AnalysisDate = analysisDate,
                LoadingPointId = source.LoadingPointID,
                QualityValue = 30.1234m
            });
            Assert.Equal(QualityWriteErrorCode.Validation, excessiveScale.ErrorCode);
            Assert.Contains(
                nameof(CreateQualityResultCommand.QualityValue),
                excessiveScale.ValidationErrors.Keys);

            var missingUpdate = await service.UpdateAsync(new UpdateQualityResultCommand
            {
                QualityResultId = long.MaxValue,
                AnalysisDate = analysisDate,
                LoadingPointId = source.LoadingPointID,
                QualityValue = 30.125m
            });
            Assert.Equal(QualityWriteErrorCode.NotFound, missingUpdate.ErrorCode);

            var options = await service.GetFormOptionsAsync();
            Assert.Contains(
                options.LoadingPoints,
                option => option.Value == source.LoadingPointID);

            var during = await CaptureSnapshotAsync(context);
            Assert.Equal(before.Trips, during.Trips);
            Assert.Equal(before.Trucks, during.Trucks);
            Assert.Equal(before.LoadingPoints, during.LoadingPoints);
            Assert.Equal(before.QualityResults.Count + 1, during.QualityResults.Count);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verificationContext = CreateContext();
        var after = await CaptureSnapshotAsync(verificationContext);
        Assert.Equal(before, after);
        Assert.False(await verificationContext.QualityResults
            .AsNoTracking()
            .AnyAsync(result => result.Comments != null && result.Comments.StartsWith(marker)));
        if (createdId.HasValue)
        {
            Assert.False(await verificationContext.QualityResults
                .AsNoTracking()
                .AnyAsync(result => result.QualityResultID == createdId.Value));
        }
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

    private static async Task<DatabaseSnapshot> CaptureSnapshotAsync(
        OcpProductionDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(
                QualityResultID, AnalysisDate, TripID, LoadingPointID, QualityValue,
                QualityUnit, TargetValue, Status, Comments, CreatedAt))
            FROM dbo.QualityResult;

            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(
                TripID, TripDate, ShiftID, TruckID, DriverID, LoadingPointID,
                DestinationID, StartTime, EndTime, Tonnage, Status, Comments, CreatedAt))
            FROM dbo.Trip;

            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(
                TruckID, TruckCode, RegistrationNumber, Model, RatedPayloadTonnes,
                Status, IsActive, CreatedAt))
            FROM dbo.Truck;

            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(
                LoadingPointID, LoadingPointCode, LoadingPointName, ZoneName,
                LoadingPointDescription, IsActive))
            FROM dbo.LoadingPoint;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        var qualityResults = await ReadFingerprintAsync(reader);
        Assert.True(await reader.NextResultAsync());
        var trips = await ReadFingerprintAsync(reader);
        Assert.True(await reader.NextResultAsync());
        var trucks = await ReadFingerprintAsync(reader);
        Assert.True(await reader.NextResultAsync());
        var loadingPoints = await ReadFingerprintAsync(reader);

        return new DatabaseSnapshot(qualityResults, trips, trucks, loadingPoints);
    }

    private static async Task<TableFingerprint> ReadFingerprintAsync(
        System.Data.Common.DbDataReader reader)
    {
        Assert.True(await reader.ReadAsync());
        return new TableFingerprint(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    private sealed record TableFingerprint(long Count, int? Checksum);

    private sealed record DatabaseSnapshot(
        TableFingerprint QualityResults,
        TableFingerprint Trips,
        TableFingerprint Trucks,
        TableFingerprint LoadingPoints);
}
