using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcpProduction.Application.ReferenceData.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.ReferenceData;
using Xunit;

namespace OcpProduction.Tests.ReferenceData;

public sealed class ReferenceDataServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task Reference_data_writes_are_compatible_and_fully_rolled_back()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await using var context = CreateContext();
        var service = new ReferenceDataService(context);
        var before = await GetFingerprintAsync(context);
        await using var transaction = await context.Database.BeginTransactionAsync();

        var workShift = await service.CreateWorkShiftAsync(new CreateWorkShiftCommand(
            $"TST{suffix[..5]}", "Poste test", new TimeOnly(6, 0), new TimeOnly(14, 0), true));
        Assert.True(workShift.Succeeded, workShift.Message);
        Assert.True((await service.UpdateWorkShiftAsync(new UpdateWorkShiftCommand(
            workShift.EntityId!.Value, "Poste test modifié", new TimeOnly(7, 0), new TimeOnly(15, 0), false))).Succeeded);
        Assert.True((await service.SetWorkShiftActiveAsync(workShift.EntityId.Value, true)).Succeeded);
        Assert.False((await service.CreateWorkShiftAsync(new CreateWorkShiftCommand(
            $"TST{suffix[..5]}", "Doublon", new TimeOnly(6, 0), new TimeOnly(14, 0), true))).Succeeded);

        var loadingPoint = await service.CreateLoadingPointAsync(new CreateLoadingPointCommand(
            $"LP{suffix[..6]}", "Point test", "Zone test", "Créé en rollback", true));
        Assert.True(loadingPoint.Succeeded, loadingPoint.Message);
        Assert.True((await service.UpdateLoadingPointAsync(new UpdateLoadingPointCommand(
            loadingPoint.EntityId!.Value, "Point test modifié", "Zone B", null, false))).Succeeded);
        Assert.True((await service.SetLoadingPointActiveAsync(loadingPoint.EntityId.Value, true)).Succeeded);

        var destination = await service.CreateDestinationAsync(new CreateDestinationCommand(
            $"DS{suffix[..6]}", "Destination test", DestinationTypes.Other, true));
        Assert.True(destination.Succeeded, destination.Message);
        Assert.True((await service.UpdateDestinationAsync(new UpdateDestinationCommand(
            destination.EntityId!.Value, "Destination test modifiée", DestinationTypes.Plant, false))).Succeeded);
        Assert.True((await service.SetDestinationActiveAsync(destination.EntityId.Value, true)).Succeeded);
        Assert.False((await service.CreateDestinationAsync(new CreateDestinationCommand(
            $"DX{suffix[..6]}", "Destination invalide", "Invalid", true))).Succeeded);

        var stopReason = await service.CreateStopReasonAsync(new CreateStopReasonCommand(
            $"RS{suffix[..6]}", "Motif test", StopReasonCategories.Operational, false, true));
        Assert.True(stopReason.Succeeded, stopReason.Message);
        Assert.True((await service.UpdateStopReasonAsync(new UpdateStopReasonCommand(
            stopReason.EntityId!.Value, "Motif test modifié", StopReasonCategories.Maintenance, true, false))).Succeeded);
        Assert.True((await service.SetStopReasonActiveAsync(stopReason.EntityId.Value, true)).Succeeded);
        Assert.False((await service.CreateStopReasonAsync(new CreateStopReasonCommand(
            $"RX{suffix[..6]}", "Motif invalide", "Invalid", false, true))).Succeeded);

        var usedShift = (await service.GetWorkShiftsAsync()).First(item => !item.CanEdit);
        Assert.False((await service.UpdateWorkShiftAsync(new UpdateWorkShiftCommand(
            usedShift.Id, "Modification refusée", usedShift.StartTime, usedShift.EndTime, usedShift.IsActive))).Succeeded);

        await transaction.RollbackAsync();
        await using var afterContext = CreateContext();
        Assert.Equal(before, await GetFingerprintAsync(afterContext));
    }

    private static OcpProductionDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>(optional: false).Build();
        var connectionString = configuration.GetConnectionString("OcpDatabase");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        return new OcpProductionDbContext(
            new DbContextOptionsBuilder<OcpProductionDbContext>().UseSqlServer(connectionString).Options);
    }

    private static async Task<ReferenceFingerprint> GetFingerprintAsync(OcpProductionDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.WorkShift),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.WorkShift),
                (SELECT COUNT_BIG(*) FROM dbo.LoadingPoint),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.LoadingPoint),
                (SELECT COUNT_BIG(*) FROM dbo.Destination),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Destination),
                (SELECT COUNT_BIG(*) FROM dbo.StopReason),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.StopReason),
                (SELECT COUNT_BIG(*) FROM dbo.Trip),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.Trip),
                (SELECT COUNT_BIG(*) FROM dbo.TruckStop),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(TruckStopID,TripID,TruckID,DriverID,ShiftID,StopReasonID,StartTime,EndTime,DurationMinutes,ValidationStatus,CreatedAt)) FROM dbo.TruckStop),
                (SELECT COUNT_BIG(*) FROM dbo.QualityResult),
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.QualityResult);
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new ReferenceFingerprint(
            reader.GetInt64(0), reader.GetInt32(1),
            reader.GetInt64(2), reader.GetInt32(3),
            reader.GetInt64(4), reader.GetInt32(5),
            reader.GetInt64(6), reader.GetInt32(7),
            reader.GetInt64(8), reader.GetInt32(9),
            reader.GetInt64(10), reader.GetInt32(11),
            reader.GetInt64(12), reader.GetInt32(13));
    }

    private sealed record ReferenceFingerprint(
        long WorkShiftCount,
        int WorkShiftChecksum,
        long LoadingPointCount,
        int LoadingPointChecksum,
        long DestinationCount,
        int DestinationChecksum,
        long StopReasonCount,
        int StopReasonChecksum,
        long TripCount,
        int TripChecksum,
        long TruckStopCount,
        int TruckStopChecksum,
        long QualityResultCount,
        int QualityResultChecksum);
}
