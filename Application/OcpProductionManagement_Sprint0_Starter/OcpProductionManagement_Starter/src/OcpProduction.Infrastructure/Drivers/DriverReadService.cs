using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Drivers.Dtos;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Drivers.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Drivers;

public sealed class DriverReadService(OcpProductionDbContext context) : IDriverReadService
{
    public async Task<PagedResult<DriverListItemDto>> SearchAsync(
        DriverSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var normalized = criteria.Normalize();
        IQueryable<Driver> query = context.Drivers.AsNoTracking();

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(driver =>
                EF.Functions.Like(driver.DriverCode, pattern, "\\") ||
                EF.Functions.Like(driver.FullName, pattern, "\\"));
        }

        if (normalized.Status is { } status)
        {
            query = query.Where(driver => driver.Status == status);
        }

        var totalItems = await query.LongCountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderBy(driver => driver.DriverCode)
            .ThenBy(driver => driver.DriverID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(driver => new DriverListItemDto
            {
                DriverId = driver.DriverID,
                DriverCode = driver.DriverCode,
                DisplayName = driver.FullName,
                Status = driver.Status,
                IsActive = driver.IsActive,
                TripCount = driver.Trips.LongCount(),
                StopCount = driver.TruckStops.LongCount(),
                LastTripActivity = driver.Trips.Max(trip => (DateTime?)trip.StartTime),
                LastStopActivity = driver.TruckStops.Max(stop => (DateTime?)stop.StartTime)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<DriverListItemDto>.Create(items, page, normalized.PageSize, totalItems);
    }

    public Task<DriverDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from driver in context.Drivers.AsNoTracking()
            join performance in context.vw_DriverPerformances.AsNoTracking()
                on driver.DriverID equals performance.DriverID into performanceRows
            from performance in performanceRows.DefaultIfEmpty()
            where driver.DriverID == id
            select new DriverDetailsDto
            {
                DriverId = driver.DriverID,
                DriverCode = driver.DriverCode,
                DisplayName = driver.FullName,
                Status = driver.Status,
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt,
                TripCount = performance.TotalTrips ?? 0,
                CompletedTripCount = performance.CompletedTrips ?? 0,
                TotalTonnage = performance.TotalTonnage,
                AverageTripDurationMinutes = performance.AverageTripDurationMinutes,
                FirstTripDate = performance.FirstTripDate,
                LastTripDate = performance.LastTripDate,
                StopCount = driver.TruckStops.LongCount(),
                TotalStopDurationMinutes = performance.TotalLinkedStopDurationMinutes,
                AverageStopDurationMinutes = performance.AverageLinkedStopDurationMinutes
            };

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DriverFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await context.Drivers.AsNoTracking()
            .Select(driver => driver.Status)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);
        return new DriverFilterOptions { Statuses = statuses };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
