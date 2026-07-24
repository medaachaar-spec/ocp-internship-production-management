using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;
using OcpProduction.Infrastructure.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripWriteServiceIntegrationTests
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
        Assert.NotEmpty(options.LoadingPoints);
        Assert.NotEmpty(options.Destinations);
        Assert.Equal(
            options.Trucks.OrderBy(option => option.Label, StringComparer.Ordinal).Select(option => option.Label),
            options.Trucks.Select(option => option.Label));
        Assert.Equal(
            options.Drivers.OrderBy(option => option.Label, StringComparer.Ordinal).Select(option => option.Label),
            options.Drivers.Select(option => option.Label));
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Create_rejects_invalid_foreign_key_without_writing()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var options = await service.GetFormOptionsAsync();
        var before = await GetFingerprintAsync(context);

        var result = await service.CreateAsync(new CreateTripCommand
        {
            TripDate = new DateOnly(2026, 7, 20),
            WorkShiftId = Assert.Single(options.WorkShifts.Take(1)).Value,
            TruckId = int.MaxValue,
            DriverId = Assert.Single(options.Drivers.Take(1)).Value,
            LoadingPointId = Assert.Single(options.LoadingPoints.Take(1)).Value,
            DestinationId = Assert.Single(options.Destinations.Take(1)).Value,
            StartTime = new DateTime(2026, 7, 20, 8, 0, 0)
        });

        var after = await GetFingerprintAsync(context);
        Assert.False(result.Success);
        Assert.Equal(TripWriteErrorCode.Validation, result.ErrorCode);
        Assert.Contains(nameof(CreateTripCommand.TruckId), result.ValidationErrors.Keys);
        Assert.Equal(before, after);
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Update_rejects_incoherent_time_and_missing_trip_without_writing()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var existing = await context.Trips
            .AsNoTracking()
            .Where(trip => trip.Status == "InProgress")
            .OrderBy(trip => trip.TripID)
            .Select(trip => new
            {
                trip.TripID,
                trip.TripDate,
                trip.ShiftID,
                trip.TruckID,
                trip.DriverID,
                trip.LoadingPointID,
                trip.DestinationID,
                trip.StartTime,
                trip.Tonnage,
                trip.Comments
            })
            .FirstAsync();
        var before = await GetFingerprintAsync(context);

        var invalidTime = await service.UpdateAsync(new UpdateTripCommand
        {
            TripId = existing.TripID,
            TripDate = existing.TripDate,
            WorkShiftId = existing.ShiftID,
            TruckId = existing.TruckID,
            DriverId = existing.DriverID,
            LoadingPointId = existing.LoadingPointID,
            DestinationId = existing.DestinationID,
            StartTime = existing.StartTime,
            EndTime = existing.StartTime.AddSeconds(-1),
            Tonnage = existing.Tonnage,
            Comments = existing.Comments
        });
        var missing = await service.UpdateAsync(new UpdateTripCommand
        {
            TripId = long.MaxValue,
            TripDate = existing.TripDate,
            WorkShiftId = existing.ShiftID,
            TruckId = existing.TruckID,
            DriverId = existing.DriverID,
            LoadingPointId = existing.LoadingPointID,
            DestinationId = existing.DestinationID,
            StartTime = existing.StartTime
        });

        var after = await GetFingerprintAsync(context);
        Assert.Equal(TripWriteErrorCode.Validation, invalidTime.ErrorCode);
        Assert.Contains(nameof(UpdateTripCommand.EndTime), invalidTime.ValidationErrors.Keys);
        Assert.Equal(TripWriteErrorCode.NotFound, missing.ErrorCode);
        Assert.Equal(before, after);
    }

    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Create_and_update_map_only_authorized_fields_then_transaction_rolls_back()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var options = await service.GetFormOptionsAsync();
        var before = await GetFingerprintAsync(context);
        var marker = $"CODEx-Sprint1-Part2-{Guid.NewGuid():N}";
        long createdTripId = 0;

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var startTime = new DateTime(2026, 7, 20, 9, 30, 0);
                var createResult = await service.CreateAsync(new CreateTripCommand
                {
                    TripDate = new DateOnly(2026, 7, 20),
                    WorkShiftId = options.WorkShifts[0].Value,
                    TruckId = options.Trucks[0].Value,
                    DriverId = options.Drivers[0].Value,
                    LoadingPointId = options.LoadingPoints[0].Value,
                    DestinationId = options.Destinations[0].Value,
                    StartTime = startTime,
                    Comments = $"{marker}-created"
                });

                Assert.True(createResult.Success);
                createdTripId = Assert.IsType<long>(createResult.TripId);

                context.ChangeTracker.Clear();
                var created = await context.Trips
                    .AsNoTracking()
                    .SingleAsync(trip => trip.TripID == createdTripId);
                var protectedCreatedAt = created.CreatedAt;

                Assert.Equal("InProgress", created.Status);
                Assert.Null(created.EndTime);
                Assert.Null(created.Tonnage);
                Assert.NotEqual(default, protectedCreatedAt);

                var updateResult = await service.UpdateAsync(new UpdateTripCommand
                {
                    TripId = createdTripId,
                    TripDate = new DateOnly(2026, 7, 21),
                    WorkShiftId = options.WorkShifts[^1].Value,
                    TruckId = options.Trucks[^1].Value,
                    DriverId = options.Drivers[^1].Value,
                    LoadingPointId = options.LoadingPoints[^1].Value,
                    DestinationId = options.Destinations[^1].Value,
                    StartTime = startTime.AddHours(1),
                    EndTime = startTime.AddHours(1).AddMinutes(15),
                    Tonnage = 1.25m,
                    Comments = $"{marker}-updated"
                });

                Assert.True(updateResult.Success);
                context.ChangeTracker.Clear();

                var updated = await context.Trips
                    .AsNoTracking()
                    .SingleAsync(trip => trip.TripID == createdTripId);
                Assert.Equal(new DateOnly(2026, 7, 21), updated.TripDate);
                Assert.Equal(options.Trucks[^1].Value, updated.TruckID);
                Assert.Equal(startTime.AddHours(1).AddMinutes(15), updated.EndTime);
                Assert.Equal(1.25m, updated.Tonnage);
                Assert.Equal($"{marker}-updated", updated.Comments);
                Assert.Equal("InProgress", updated.Status);
                Assert.Equal(protectedCreatedAt, updated.CreatedAt);
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

        var tripEntityType = context.Model.FindEntityType(typeof(Trip));
        Assert.NotNull(tripEntityType);
        Assert.DoesNotContain(tripEntityType.GetProperties(), property => property.IsConcurrencyToken);
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

    private static TripWriteService CreateService(OcpProductionDbContext context) =>
        new(context, NullLogger<TripWriteService>.Instance);

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
