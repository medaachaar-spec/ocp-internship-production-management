using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OcpProduction.Application.Trips;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripWorkflowServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Closure_validation_and_edit_locks_are_enforced_then_transaction_rolls_back()
    {
        await using var context = CreateContext();
        var writeService = new TripWriteService(context, NullLogger<TripWriteService>.Instance);
        var workflowService = new TripWorkflowService(
            context,
            NullLogger<TripWorkflowService>.Instance);
        var options = await writeService.GetFormOptionsAsync();
        var before = await GetFingerprintAsync(context);
        var marker = $"OCP-TRIP-WORKFLOW-ROLLBACK-{Guid.NewGuid():N}";
        long createdTripId = 0;

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var startTime = new DateTime(2026, 7, 20, 10, 0, 0);
                var createResult = await writeService.CreateAsync(new CreateTripCommand
                {
                    TripDate = new DateOnly(2026, 7, 20),
                    WorkShiftId = options.WorkShifts[0].Value,
                    TruckId = options.Trucks[0].Value,
                    DriverId = options.Drivers[0].Value,
                    LoadingPointId = options.LoadingPoints[0].Value,
                    DestinationId = options.Destinations[0].Value,
                    StartTime = startTime,
                    Comments = marker
                });

                Assert.True(createResult.Success);
                createdTripId = Assert.IsType<long>(createResult.TripId);
                context.ChangeTracker.Clear();

                var created = await context.Trips
                    .AsNoTracking()
                    .SingleAsync(trip => trip.TripID == createdTripId);
                var protectedSnapshot = new
                {
                    created.TripDate,
                    created.ShiftID,
                    created.TruckID,
                    created.DriverID,
                    created.LoadingPointID,
                    created.DestinationID,
                    created.StartTime,
                    created.Comments,
                    created.CreatedAt
                };

                Assert.Equal(TripStatuses.InProgress, created.Status);

                var directValidation = await workflowService.ValidateAsync(createdTripId);
                Assert.Equal(TripWorkflowErrorCode.InvalidState, directValidation.ErrorCode);

                var incoherentClosure = await workflowService.CloseAsync(new CloseTripCommand
                {
                    TripId = createdTripId,
                    EndTime = startTime.AddSeconds(-1),
                    Tonnage = 12.34m
                });
                Assert.Equal(TripWorkflowErrorCode.Validation, incoherentClosure.ErrorCode);
                Assert.Contains(nameof(CloseTripCommand.EndTime), incoherentClosure.ValidationErrors.Keys);

                var closure = await workflowService.CloseAsync(new CloseTripCommand
                {
                    TripId = createdTripId,
                    EndTime = startTime,
                    Tonnage = 12.34m
                });
                Assert.True(closure.Success);
                context.ChangeTracker.Clear();

                var pending = await context.Trips
                    .AsNoTracking()
                    .SingleAsync(trip => trip.TripID == createdTripId);
                Assert.Equal(TripStatuses.PendingValidation, pending.Status);
                Assert.Equal(startTime, pending.EndTime);
                Assert.Equal(12.34m, pending.Tonnage);
                Assert.Equal(protectedSnapshot, new
                {
                    pending.TripDate,
                    pending.ShiftID,
                    pending.TruckID,
                    pending.DriverID,
                    pending.LoadingPointID,
                    pending.DestinationID,
                    pending.StartTime,
                    pending.Comments,
                    pending.CreatedAt
                });

                var queue = await workflowService.GetPendingValidationAsync(
                    new TripValidationSearchCriteria { Search = createdTripId.ToString() });
                Assert.Equal(createdTripId, Assert.Single(queue.Items).TripId);

                var pendingEdit = await writeService.UpdateAsync(CreateUpdateCommand(pending));
                Assert.Equal(TripWriteErrorCode.InvalidState, pendingEdit.ErrorCode);

                var validation = await workflowService.ValidateAsync(createdTripId);
                Assert.True(validation.Success);
                context.ChangeTracker.Clear();

                var completed = await context.Trips
                    .AsNoTracking()
                    .SingleAsync(trip => trip.TripID == createdTripId);
                Assert.Equal(TripStatuses.Completed, completed.Status);
                Assert.Equal(startTime, completed.EndTime);
                Assert.Equal(12.34m, completed.Tonnage);
                Assert.Equal(protectedSnapshot, new
                {
                    completed.TripDate,
                    completed.ShiftID,
                    completed.TruckID,
                    completed.DriverID,
                    completed.LoadingPointID,
                    completed.DestinationID,
                    completed.StartTime,
                    completed.Comments,
                    completed.CreatedAt
                });

                var repeatedValidation = await workflowService.ValidateAsync(createdTripId);
                var repeatedClosure = await workflowService.CloseAsync(new CloseTripCommand
                {
                    TripId = createdTripId,
                    EndTime = startTime,
                    Tonnage = 12.34m
                });
                var completedEdit = await writeService.UpdateAsync(CreateUpdateCommand(completed));
                var missing = await workflowService.ValidateAsync(long.MaxValue);

                Assert.Equal(TripWorkflowErrorCode.InvalidState, repeatedValidation.ErrorCode);
                Assert.Equal(TripWorkflowErrorCode.InvalidState, repeatedClosure.ErrorCode);
                Assert.Equal(TripWriteErrorCode.InvalidState, completedEdit.ErrorCode);
                Assert.Equal(TripWorkflowErrorCode.NotFound, missing.ErrorCode);
                Assert.False(await context.TruckStops.AnyAsync(stop => stop.TripID == createdTripId));
                Assert.False(await context.QualityResults.AnyAsync(result => result.TripID == createdTripId));
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.Trips.AsNoTracking().AnyAsync(trip => trip.TripID == createdTripId));
        var after = await GetFingerprintAsync(context);
        Assert.Equal(before, after);
    }

    private static UpdateTripCommand CreateUpdateCommand(
        OcpProduction.Infrastructure.Generated.Trip trip) => new()
        {
            TripId = trip.TripID,
            TripDate = trip.TripDate,
            WorkShiftId = trip.ShiftID,
            TruckId = trip.TruckID,
            DriverId = trip.DriverID,
            LoadingPointId = trip.LoadingPointID,
            DestinationId = trip.DestinationID,
            StartTime = trip.StartTime,
            EndTime = trip.EndTime,
            Tonnage = trip.Tonnage,
            Comments = trip.Comments
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

    private static async Task<TripFingerprint> GetFingerprintAsync(
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
                    TripID,
                    TripDate,
                    ShiftID,
                    TruckID,
                    DriverID,
                    LoadingPointID,
                    DestinationID,
                    StartTime,
                    EndTime,
                    Tonnage,
                    Status,
                    Comments,
                    CreatedAt))
            FROM dbo.Trip;
            """;

        if (context.Database.CurrentTransaction is { } transaction)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new TripFingerprint(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    private sealed record TripFingerprint(long Count, int? Checksum);
}
