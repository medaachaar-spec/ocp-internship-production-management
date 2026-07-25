using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopWriteServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Form_options_are_active_selectable_and_ordered()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var options = await service.GetFormOptionsAsync();

        Assert.NotEmpty(options.Trucks);
        Assert.NotEmpty(options.Drivers);
        Assert.NotEmpty(options.WorkShifts);
        Assert.NotEmpty(options.StopReasons);
        Assert.Equal(
            options.Trucks.OrderBy(option => option.Label, StringComparer.Ordinal).Select(option => option.Label),
            options.Trucks.Select(option => option.Label));
        Assert.Equal(
            options.StopReasons.OrderBy(option => option.Label, StringComparer.Ordinal).Select(option => option.Label),
            options.StopReasons.Select(option => option.Label));
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Create_rejects_missing_references_and_trip_mismatches_without_writing()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var options = await service.GetFormOptionsAsync();
        var before = await GetDatabaseFingerprintAsync(context);
        var startTime = new DateTime(2026, 7, 20, 8, 0, 0);

        var missing = await service.CreateAsync(new CreateStopCommand
        {
            TripId = long.MaxValue,
            TruckId = int.MaxValue,
            DriverId = int.MaxValue,
            WorkShiftId = int.MaxValue,
            StopReasonId = int.MaxValue,
            StartTime = startTime
        });

        Assert.Equal(StopWriteErrorCode.Validation, missing.ErrorCode);
        Assert.Contains(nameof(CreateStopCommand.TripId), missing.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.TruckId), missing.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.DriverId), missing.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.WorkShiftId), missing.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.StopReasonId), missing.ValidationErrors.Keys);

        var trip = await context.Trips
            .AsNoTracking()
            .Where(candidate =>
                candidate.TripID > 0 &&
                candidate.Truck.IsActive &&
                (candidate.Truck.Status == "Available" || candidate.Truck.Status == "InService") &&
                candidate.Driver.IsActive && candidate.Driver.Status == "Active" &&
                candidate.Shift.IsActive)
            .OrderBy(candidate => candidate.TripID)
            .Select(candidate => new
            {
                candidate.TripID,
                candidate.TruckID,
                candidate.DriverID,
                candidate.ShiftID
            })
            .FirstAsync();

        var otherTruck = Assert.Single(options.Trucks.Where(option => option.Value != trip.TruckID).Take(1));
        var otherDriver = Assert.Single(options.Drivers.Where(option => option.Value != trip.DriverID).Take(1));
        var otherShift = Assert.Single(options.WorkShifts.Where(option => option.Value != trip.ShiftID).Take(1));
        var mismatch = await service.CreateAsync(new CreateStopCommand
        {
            TripId = trip.TripID,
            TruckId = otherTruck.Value,
            DriverId = otherDriver.Value,
            WorkShiftId = otherShift.Value,
            StopReasonId = options.StopReasons[0].Value,
            StartTime = startTime
        });

        Assert.Equal(StopWriteErrorCode.Validation, mismatch.ErrorCode);
        Assert.Contains(nameof(CreateStopCommand.TruckId), mismatch.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.DriverId), mismatch.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateStopCommand.WorkShiftId), mismatch.ValidationErrors.Keys);
        Assert.Equal(before, await GetDatabaseFingerprintAsync(context));
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Missing_and_existing_non_editable_stops_are_rejected_without_writing()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var options = await service.GetFormOptionsAsync();
        var existing = await context.TruckStops
            .AsNoTracking()
            .Where(stop => stop.EndTime != null || stop.ValidationStatus != StopValidationStatuses.Pending)
            .OrderBy(stop => stop.TruckStopID)
            .FirstAsync();
        var before = await GetDatabaseFingerprintAsync(context);

        var missing = await service.UpdateAsync(CreateUpdateCommand(
            long.MaxValue,
            options,
            new DateTime(2026, 7, 20, 8, 0, 0)));
        var locked = await service.UpdateAsync(CreateUpdateCommand(
            existing.TruckStopID,
            options,
            existing.StartTime));

        Assert.Equal(StopWriteErrorCode.NotFound, missing.ErrorCode);
        Assert.Equal(StopWriteErrorCode.InvalidState, locked.ErrorCode);
        Assert.Equal(before, await GetDatabaseFingerprintAsync(context));
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Create_update_and_edit_locks_are_verified_then_transaction_rolls_back()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var options = await service.GetFormOptionsAsync();
        var before = await GetDatabaseFingerprintAsync(context);
        var marker = $"OCP-STOP-WRITE-ROLLBACK-{Guid.NewGuid():N}";
        long createdStopId = 0;

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var startTime = new DateTime(2026, 7, 20, 9, 30, 0);
                var createResult = await service.CreateAsync(new CreateStopCommand
                {
                    TruckId = options.Trucks[0].Value,
                    DriverId = options.Drivers[0].Value,
                    WorkShiftId = options.WorkShifts[0].Value,
                    StopReasonId = options.StopReasons[0].Value,
                    StartTime = startTime,
                    Comments = $"  {marker}-created  "
                });

                Assert.True(createResult.Success);
                createdStopId = Assert.IsType<long>(createResult.StopId);

                context.ChangeTracker.Clear();
                var created = await context.TruckStops
                    .AsNoTracking()
                    .SingleAsync(stop => stop.TruckStopID == createdStopId);
                var protectedCreatedAt = created.CreatedAt;

                Assert.Null(created.TripID);
                Assert.Null(created.EndTime);
                Assert.Null(created.DurationMinutes);
                Assert.Equal(StopValidationStatuses.Pending, created.ValidationStatus);
                Assert.Equal($"{marker}-created", created.Comments);
                Assert.NotEqual(default, protectedCreatedAt);

                var updateResult = await service.UpdateAsync(new UpdateStopCommand
                {
                    StopId = createdStopId,
                    TruckId = options.Trucks[^1].Value,
                    DriverId = options.Drivers[^1].Value,
                    WorkShiftId = options.WorkShifts[^1].Value,
                    StopReasonId = options.StopReasons[^1].Value,
                    StartTime = startTime.AddMinutes(5),
                    Comments = $"{marker}-updated"
                });

                Assert.True(updateResult.Success);
                context.ChangeTracker.Clear();
                var updated = await context.TruckStops.SingleAsync(
                    stop => stop.TruckStopID == createdStopId);
                Assert.Equal(options.Trucks[^1].Value, updated.TruckID);
                Assert.Equal(options.Drivers[^1].Value, updated.DriverID);
                Assert.Equal(options.WorkShifts[^1].Value, updated.ShiftID);
                Assert.Equal(options.StopReasons[^1].Value, updated.StopReasonID);
                Assert.Equal(startTime.AddMinutes(5), updated.StartTime);
                Assert.Equal($"{marker}-updated", updated.Comments);
                Assert.Null(updated.EndTime);
                Assert.Equal(StopValidationStatuses.Pending, updated.ValidationStatus);
                Assert.Equal(protectedCreatedAt, updated.CreatedAt);

                updated.EndTime = updated.StartTime.AddMinutes(10);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                var closedResult = await service.UpdateAsync(CreateUpdateCommand(
                    createdStopId,
                    options,
                    startTime.AddMinutes(5)));
                Assert.Equal(StopWriteErrorCode.InvalidState, closedResult.ErrorCode);

                var testSetup = await context.TruckStops.SingleAsync(
                    stop => stop.TruckStopID == createdStopId);
                testSetup.EndTime = null;
                testSetup.ValidationStatus = StopValidationStatuses.Validated;
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                var validatedResult = await service.UpdateAsync(CreateUpdateCommand(
                    createdStopId,
                    options,
                    startTime.AddMinutes(5)));
                Assert.Equal(StopWriteErrorCode.InvalidState, validatedResult.ErrorCode);
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.TruckStops.AsNoTracking().AnyAsync(
            stop => stop.TruckStopID == createdStopId));
        Assert.False(await context.TruckStops.AsNoTracking().AnyAsync(
            stop => stop.Comments != null && stop.Comments.Contains(marker)));
        Assert.Equal(before, await GetDatabaseFingerprintAsync(context));
    }

    private static UpdateStopCommand CreateUpdateCommand(
        long stopId,
        StopFormOptions options,
        DateTime startTime) => new()
        {
            StopId = stopId,
            TruckId = options.Trucks[0].Value,
            DriverId = options.Drivers[0].Value,
            WorkShiftId = options.WorkShifts[0].Value,
            StopReasonId = options.StopReasons[0].Value,
            StartTime = startTime,
            Comments = "Test sans écriture persistante"
        };

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

    private static StopWriteService CreateService(OcpProductionDbContext context) =>
        new(context, NullLogger<StopWriteService>.Instance);

    private static async Task<DatabaseFingerprint> GetDatabaseFingerprintAsync(
        OcpProductionDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(TruckStopID, TruckID, DriverID, ShiftID, TripID, StopReasonID, StartTime, EndTime, DurationMinutes, ValidationStatus, Comments, CreatedAt)) FROM dbo.TruckStop;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(TripID, TripDate, ShiftID, TruckID, DriverID, LoadingPointID, DestinationID, StartTime, EndTime, Tonnage, Status, Comments, CreatedAt)) FROM dbo.Trip;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(TruckID, TruckCode, RegistrationNumber, Model, RatedPayloadTonnes, Status, IsActive, CreatedAt)) FROM dbo.Truck;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(DriverID, DriverCode, FullName, LicenseNumber, Status, IsActive, CreatedAt)) FROM dbo.Driver;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(ShiftID, ShiftCode, ShiftName, StartTime, EndTime, IsActive)) FROM dbo.WorkShift;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(StopReasonID, ReasonCode, ReasonName, Category, IsPlanned, IsActive)) FROM dbo.StopReason;
            SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(QualityResultID, AnalysisDate, TripID, LoadingPointID, QualityValue, QualityUnit, TargetValue, Status, Comments, CreatedAt)) FROM dbo.QualityResult;
            """;

        if (context.Database.CurrentTransaction is { } transaction)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        await using var reader = await command.ExecuteReaderAsync();
        var fingerprints = new List<TableFingerprint>();
        do
        {
            Assert.True(await reader.ReadAsync());
            fingerprints.Add(new TableFingerprint(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? null : reader.GetInt32(1)));
        }
        while (await reader.NextResultAsync());

        Assert.Equal(7, fingerprints.Count);
        return new DatabaseFingerprint(
            fingerprints[0],
            fingerprints[1],
            fingerprints[2],
            fingerprints[3],
            fingerprints[4],
            fingerprints[5],
            fingerprints[6]);
    }

    private sealed record TableFingerprint(long Count, int? Checksum);

    private sealed record DatabaseFingerprint(
        TableFingerprint TruckStops,
        TableFingerprint Trips,
        TableFingerprint Trucks,
        TableFingerprint Drivers,
        TableFingerprint WorkShifts,
        TableFingerprint StopReasons,
        TableFingerprint QualityResults);
}
