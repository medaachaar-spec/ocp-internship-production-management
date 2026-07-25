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

public sealed class StopWorkflowServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Closure_queue_validation_and_edit_locks_are_enforced_then_transaction_rolls_back()
    {
        await using var context = CreateContext();
        var writeService = new StopWriteService(
            context,
            NullLogger<StopWriteService>.Instance);
        var workflowService = new StopWorkflowService(
            context,
            NullLogger<StopWorkflowService>.Instance);
        var options = await writeService.GetFormOptionsAsync();
        var before = await GetDatabaseFingerprintAsync(context);
        var marker = $"OCP-STOP-WORKFLOW-ROLLBACK-{Guid.NewGuid():N}";
        var createdStopIds = new List<long>();

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var firstStart = new DateTime(2026, 7, 20, 8, 0, 0);
                var secondStart = new DateTime(2026, 7, 20, 8, 30, 0);
                var firstId = await CreateStopAsync(
                    writeService,
                    options,
                    firstStart,
                    $"{marker}-A",
                    options.Trucks[0].Value);
                var secondId = await CreateStopAsync(
                    writeService,
                    options,
                    secondStart,
                    $"{marker}-B",
                    options.Trucks[^1].Value);
                createdStopIds.AddRange([firstId, secondId]);
                context.ChangeTracker.Clear();

                var firstBefore = await context.TruckStops
                    .AsNoTracking()
                    .SingleAsync(stop => stop.TruckStopID == firstId);
                var firstProtectedSnapshot = new
                {
                    firstBefore.TripID,
                    firstBefore.TruckID,
                    firstBefore.DriverID,
                    firstBefore.ShiftID,
                    firstBefore.StopReasonID,
                    firstBefore.StartTime,
                    firstBefore.Comments,
                    firstBefore.CreatedAt
                };

                var directValidation = await workflowService.ValidateAsync(firstId);
                Assert.Equal(StopWorkflowErrorCode.InvalidState, directValidation.ErrorCode);

                var incoherentClosure = await workflowService.CloseAsync(new CloseStopCommand
                {
                    StopId = firstId,
                    EndTime = firstStart.AddSeconds(-1)
                });
                Assert.Equal(StopWorkflowErrorCode.Validation, incoherentClosure.ErrorCode);
                Assert.Contains(nameof(CloseStopCommand.EndTime), incoherentClosure.ValidationErrors.Keys);

                var firstClosure = await workflowService.CloseAsync(new CloseStopCommand
                {
                    StopId = firstId,
                    EndTime = firstStart.AddHours(2)
                });
                var secondClosure = await workflowService.CloseAsync(new CloseStopCommand
                {
                    StopId = secondId,
                    EndTime = secondStart.AddMinutes(30)
                });
                Assert.True(firstClosure.Success);
                Assert.True(secondClosure.Success);
                context.ChangeTracker.Clear();

                var firstPending = await context.TruckStops
                    .AsNoTracking()
                    .SingleAsync(stop => stop.TruckStopID == firstId);
                Assert.Equal(firstStart.AddHours(2), firstPending.EndTime);
                Assert.Equal(120, firstPending.DurationMinutes);
                Assert.Equal(StopValidationStatuses.Pending, firstPending.ValidationStatus);
                Assert.Equal(firstProtectedSnapshot, new
                {
                    firstPending.TripID,
                    firstPending.TruckID,
                    firstPending.DriverID,
                    firstPending.ShiftID,
                    firstPending.StopReasonID,
                    firstPending.StartTime,
                    firstPending.Comments,
                    firstPending.CreatedAt
                });

                var queue = await workflowService.GetPendingValidationAsync(
                    new StopValidationSearchCriteria
                    {
                        Search = marker,
                        DateFrom = new DateOnly(2026, 7, 20),
                        DateTo = new DateOnly(2026, 7, 20),
                        PageSize = 10
                    });
                Assert.Equal([secondId, firstId], queue.Items.Select(stop => stop.StopId));
                Assert.All(queue.Items, stop => Assert.True(stop.EndTime >= stop.StartTime));

                var pendingEdit = await writeService.UpdateAsync(
                    CreateUpdateCommand(firstPending));
                Assert.Equal(StopWriteErrorCode.InvalidState, pendingEdit.ErrorCode);

                var validation = await workflowService.ValidateAsync(secondId);
                Assert.True(validation.Success);
                context.ChangeTracker.Clear();

                var validated = await context.TruckStops
                    .AsNoTracking()
                    .SingleAsync(stop => stop.TruckStopID == secondId);
                Assert.Equal(StopValidationStatuses.Validated, validated.ValidationStatus);
                Assert.Equal(secondStart.AddMinutes(30), validated.EndTime);
                Assert.Equal(30, validated.DurationMinutes);

                var repeatedValidation = await workflowService.ValidateAsync(secondId);
                var repeatedClosure = await workflowService.CloseAsync(new CloseStopCommand
                {
                    StopId = secondId,
                    EndTime = secondStart.AddMinutes(45)
                });
                var validatedEdit = await writeService.UpdateAsync(
                    CreateUpdateCommand(validated));
                var missing = await workflowService.ValidateAsync(long.MaxValue);

                Assert.Equal(StopWorkflowErrorCode.InvalidState, repeatedValidation.ErrorCode);
                Assert.Equal(StopWorkflowErrorCode.InvalidState, repeatedClosure.ErrorCode);
                Assert.Equal(StopWriteErrorCode.InvalidState, validatedEdit.ErrorCode);
                Assert.Equal(StopWorkflowErrorCode.NotFound, missing.ErrorCode);

                var remainingQueue = await workflowService.GetPendingValidationAsync(
                    new StopValidationSearchCriteria { Search = marker });
                Assert.Equal(firstId, Assert.Single(remainingQueue.Items).StopId);
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.TruckStops.AsNoTracking().AnyAsync(
            stop => createdStopIds.Contains(stop.TruckStopID)));
        Assert.False(await context.TruckStops.AsNoTracking().AnyAsync(
            stop => stop.Comments != null && stop.Comments.Contains(marker)));
        Assert.Equal(before, await GetDatabaseFingerprintAsync(context));
    }

    private static async Task<long> CreateStopAsync(
        StopWriteService service,
        StopFormOptions options,
        DateTime startTime,
        string comments,
        int truckId)
    {
        var result = await service.CreateAsync(new CreateStopCommand
        {
            TruckId = truckId,
            DriverId = options.Drivers[0].Value,
            WorkShiftId = options.WorkShifts[0].Value,
            StopReasonId = options.StopReasons[0].Value,
            StartTime = startTime,
            Comments = comments
        });

        Assert.True(result.Success);
        return Assert.IsType<long>(result.StopId);
    }

    private static UpdateStopCommand CreateUpdateCommand(
        OcpProduction.Infrastructure.Generated.TruckStop stop) => new()
        {
            StopId = stop.TruckStopID,
            TripId = stop.TripID,
            TruckId = stop.TruckID,
            DriverId = stop.DriverID,
            WorkShiftId = stop.ShiftID,
            StopReasonId = stop.StopReasonID,
            StartTime = stop.StartTime,
            Comments = stop.Comments
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
