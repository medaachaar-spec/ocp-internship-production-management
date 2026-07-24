using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trucks.Dtos;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Application.Trucks.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Trucks;

public sealed class TruckReadService(OcpProductionDbContext context) : ITruckReadService
{
    public async Task<PagedResult<TruckListItemDto>> SearchAsync(
        TruckSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var normalized = criteria.Normalize();
        IQueryable<Truck> query = context.Trucks.AsNoTracking();

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(truck =>
                EF.Functions.Like(truck.TruckCode, pattern, "\\") ||
                (truck.RegistrationNumber != null && EF.Functions.Like(truck.RegistrationNumber, pattern, "\\")) ||
                (truck.Model != null && EF.Functions.Like(truck.Model, pattern, "\\")));
        }

        if (normalized.Status is { } status)
        {
            query = query.Where(truck => truck.Status == status);
        }

        if (normalized.Model is { } model)
        {
            query = query.Where(truck => truck.Model == model);
        }

        var totalItems = await query.LongCountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderBy(truck => truck.TruckCode)
            .ThenBy(truck => truck.TruckID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(truck => new TruckListItemDto
            {
                TruckId = truck.TruckID,
                TruckCode = truck.TruckCode,
                RegistrationNumber = truck.RegistrationNumber,
                Model = truck.Model,
                RatedPayloadTonnes = truck.RatedPayloadTonnes,
                Status = truck.Status,
                IsActive = truck.IsActive,
                TripCount = truck.Trips.LongCount(),
                StopCount = truck.TruckStops.LongCount(),
                LastTripActivity = truck.Trips.Max(trip => (DateTime?)trip.StartTime),
                LastStopActivity = truck.TruckStops.Max(stop => (DateTime?)stop.StartTime)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<TruckListItemDto>.Create(items, page, normalized.PageSize, totalItems);
    }

    public Task<TruckDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from truck in context.Trucks.AsNoTracking()
            join performance in context.vw_TruckPerformances.AsNoTracking()
                on truck.TruckID equals performance.TruckID into performanceRows
            from performance in performanceRows.DefaultIfEmpty()
            where truck.TruckID == id
            select new TruckDetailsDto
            {
                TruckId = truck.TruckID,
                TruckCode = truck.TruckCode,
                RegistrationNumber = truck.RegistrationNumber,
                Model = truck.Model,
                RatedPayloadTonnes = truck.RatedPayloadTonnes,
                Status = truck.Status,
                IsActive = truck.IsActive,
                CreatedAt = truck.CreatedAt,
                TripCount = performance.TotalTrips ?? 0,
                CompletedTripCount = performance.CompletedTrips ?? 0,
                TotalTonnage = performance.TotalTonnage,
                AverageTripDurationMinutes = performance.AverageTripDurationMinutes,
                FirstTripDate = performance.FirstTripDate,
                LastTripDate = performance.LastTripDate,
                StopCount = performance.TotalStopCount ?? 0,
                TotalDowntimeMinutes = performance.TotalDowntimeMinutes,
                AverageStopDurationMinutes = performance.AverageStopDurationMinutes
            };

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TruckFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await context.Trucks.AsNoTracking()
            .Select(truck => truck.Status)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);
        var models = await context.Trucks.AsNoTracking()
            .Where(truck => truck.Model != null)
            .Select(truck => truck.Model!)
            .Distinct()
            .OrderBy(model => model)
            .ToListAsync(cancellationToken);

        return new TruckFilterOptions { Statuses = statuses, Models = models };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
