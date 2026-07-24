using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Performance;

public sealed class PerformanceReadService(OcpProductionDbContext context) : IPerformanceReadService
{
    public async Task<PagedResult<TruckPerformanceListItemDto>> SearchTrucksAsync(
        TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var normalized = criteria.Normalize();
        var query = ApplyTruckFilters(context.vw_TruckPerformances.AsNoTracking(), normalized);
        var totalItems = await query.LongCountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));
        var items = await ApplyTruckSort(query, normalized.Sort)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(performance => new TruckPerformanceListItemDto
            {
                TruckId = performance.TruckID,
                TruckCode = performance.TruckCode,
                TruckModel = performance.TruckModel,
                TruckStatus = performance.TruckStatus,
                TotalTrips = performance.TotalTrips ?? 0,
                CompletedTrips = performance.CompletedTrips ?? 0,
                CompletionRatePercent = performance.CompletionRatePercent,
                TotalTonnage = performance.TotalTonnage,
                AveragePayloadUtilizationPercent = performance.AveragePayloadUtilizationPercent,
                TotalStopCount = performance.TotalStopCount,
                TotalDowntimeMinutes = performance.TotalDowntimeMinutes,
                AverageP2O5 = performance.AverageP2O5,
                LastTripDate = performance.LastTripDate
            })
            .ToListAsync(cancellationToken);

        return PagedResult<TruckPerformanceListItemDto>.Create(items, page, normalized.PageSize, totalItems);
    }

    public Task<TruckPerformanceDetailsDto?> GetTruckAsync(
        int truckId,
        CancellationToken cancellationToken = default)
    {
        return context.vw_TruckPerformances.AsNoTracking()
            .Where(performance => performance.TruckID == truckId)
            .Select(performance => new TruckPerformanceDetailsDto
            {
                TruckId = performance.TruckID,
                TruckCode = performance.TruckCode,
                TruckModel = performance.TruckModel,
                TruckStatus = performance.TruckStatus,
                RatedPayloadTonnes = performance.RatedPayloadTonnes,
                TotalTrips = performance.TotalTrips ?? 0,
                CompletedTrips = performance.CompletedTrips ?? 0,
                IncompleteTrips = performance.IncompleteTrips ?? 0,
                CompletionRatePercent = performance.CompletionRatePercent,
                ActiveTripDays = performance.ActiveTripDays,
                FirstTripDate = performance.FirstTripDate,
                LastTripDate = performance.LastTripDate,
                TripsPerActiveDay = performance.TripsPerActiveDay,
                HasTrips = performance.HasTrips == true,
                TotalTonnage = performance.TotalTonnage,
                AveragePayloadTonnes = performance.AveragePayloadTonnes,
                MinimumPayloadTonnes = performance.MinimumPayloadTonnes,
                MaximumPayloadTonnes = performance.MaximumPayloadTonnes,
                AveragePayloadUtilizationPercent = performance.AveragePayloadUtilizationPercent,
                AverageTripDurationMinutes = performance.AverageTripDurationMinutes,
                MinimumTripDurationMinutes = performance.MinimumTripDurationMinutes,
                MaximumTripDurationMinutes = performance.MaximumTripDurationMinutes,
                TotalStopCount = performance.TotalStopCount,
                CompletedStopCount = performance.CompletedStopCount,
                TotalDowntimeMinutes = performance.TotalDowntimeMinutes,
                AverageStopDurationMinutes = performance.AverageStopDurationMinutes,
                MaximumStopDurationMinutes = performance.MaximumStopDurationMinutes,
                StopsWithTripCount = performance.StopsWithTripCount,
                StopsWithoutTripCount = performance.StopsWithoutTripCount,
                HasStops = performance.HasStops == true,
                QualityResultCount = performance.QualityResultCount,
                AverageP2O5 = performance.AverageP2O5,
                MinimumP2O5 = performance.MinimumP2O5,
                MaximumP2O5 = performance.MaximumP2O5,
                HasQualityResults = performance.HasQualityResults == true
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<DriverPerformanceListItemDto>> SearchDriversAsync(
        DriverPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var normalized = criteria.Normalize();
        var query = ApplyDriverFilters(context.vw_DriverPerformances.AsNoTracking(), normalized);
        var totalItems = await query.LongCountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));
        var items = await ApplyDriverSort(query, normalized.Sort)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(performance => new DriverPerformanceListItemDto
            {
                DriverId = performance.DriverID,
                DriverCode = performance.DriverCode,
                DriverFullName = performance.DriverFullName,
                DriverStatus = performance.DriverStatus,
                TotalTrips = performance.TotalTrips ?? 0,
                CompletedTrips = performance.CompletedTrips ?? 0,
                CompletionRatePercent = performance.CompletionRatePercent,
                TotalTonnage = performance.TotalTonnage,
                AverageTripDurationMinutes = performance.AverageTripDurationMinutes,
                TotalLinkedStopCount = performance.TotalLinkedStopCount,
                TotalLinkedStopDurationMinutes = performance.TotalLinkedStopDurationMinutes,
                AverageP2O5 = performance.AverageP2O5,
                LastTripDate = performance.LastTripDate
            })
            .ToListAsync(cancellationToken);

        return PagedResult<DriverPerformanceListItemDto>.Create(items, page, normalized.PageSize, totalItems);
    }

    public Task<DriverPerformanceDetailsDto?> GetDriverAsync(
        int driverId,
        CancellationToken cancellationToken = default)
    {
        return context.vw_DriverPerformances.AsNoTracking()
            .Where(performance => performance.DriverID == driverId)
            .Select(performance => new DriverPerformanceDetailsDto
            {
                DriverId = performance.DriverID,
                DriverCode = performance.DriverCode,
                DriverFullName = performance.DriverFullName,
                DriverStatus = performance.DriverStatus,
                TotalTrips = performance.TotalTrips ?? 0,
                CompletedTrips = performance.CompletedTrips ?? 0,
                IncompleteTrips = performance.IncompleteTrips ?? 0,
                CompletionRatePercent = performance.CompletionRatePercent,
                ActiveTripDays = performance.ActiveTripDays,
                FirstTripDate = performance.FirstTripDate,
                LastTripDate = performance.LastTripDate,
                TripsPerActiveDay = performance.TripsPerActiveDay,
                HasTrips = performance.HasTrips == true,
                TotalTonnage = performance.TotalTonnage,
                AveragePayloadTonnes = performance.AveragePayloadTonnes,
                MinimumPayloadTonnes = performance.MinimumPayloadTonnes,
                MaximumPayloadTonnes = performance.MaximumPayloadTonnes,
                AveragePayloadUtilizationPercent = performance.AveragePayloadUtilizationPercent,
                AverageTripDurationMinutes = performance.AverageTripDurationMinutes,
                MinimumTripDurationMinutes = performance.MinimumTripDurationMinutes,
                MaximumTripDurationMinutes = performance.MaximumTripDurationMinutes,
                TripsWithStops = performance.TripsWithStops,
                TotalLinkedStopCount = performance.TotalLinkedStopCount,
                TotalLinkedStopDurationMinutes = performance.TotalLinkedStopDurationMinutes,
                AverageLinkedStopDurationMinutes = performance.AverageLinkedStopDurationMinutes,
                MaximumLinkedStopDurationMinutes = performance.MaximumLinkedStopDurationMinutes,
                HasLinkedStops = performance.HasLinkedStops == true,
                QualityResultCount = performance.QualityResultCount,
                AverageP2O5 = performance.AverageP2O5,
                MinimumP2O5 = performance.MinimumP2O5,
                MaximumP2O5 = performance.MaximumP2O5,
                HasQualityResults = performance.HasQualityResults == true
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PerformanceFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var truckStatuses = await context.vw_TruckPerformances.AsNoTracking()
            .Select(performance => performance.TruckStatus)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);
        var driverStatuses = await context.vw_DriverPerformances.AsNoTracking()
            .Select(performance => performance.DriverStatus)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);

        return new PerformanceFilterOptions
        {
            TruckStatuses = truckStatuses,
            DriverStatuses = driverStatuses
        };
    }

    private static IQueryable<vw_TruckPerformance> ApplyTruckFilters(
        IQueryable<vw_TruckPerformance> query,
        TruckPerformanceSearchCriteria criteria)
    {
        if (criteria.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(performance =>
                EF.Functions.Like(performance.TruckCode, pattern, "\\") ||
                (performance.TruckModel != null && EF.Functions.Like(performance.TruckModel, pattern, "\\")));
        }

        if (criteria.Status is { } status)
        {
            query = query.Where(performance => performance.TruckStatus == status);
        }

        return query;
    }

    private static IOrderedQueryable<vw_TruckPerformance> ApplyTruckSort(
        IQueryable<vw_TruckPerformance> query,
        string sort)
    {
        return sort switch
        {
            TruckPerformanceSort.TotalTonnageDesc => query.OrderByDescending(performance => performance.TotalTonnage ?? 0).ThenBy(performance => performance.TruckCode),
            TruckPerformanceSort.DowntimeDesc => query.OrderByDescending(performance => performance.TotalDowntimeMinutes ?? 0).ThenBy(performance => performance.TruckCode),
            TruckPerformanceSort.CompletionRateDesc => query.OrderByDescending(performance => performance.CompletionRatePercent ?? 0).ThenBy(performance => performance.TruckCode),
            TruckPerformanceSort.CodeAsc => query.OrderBy(performance => performance.TruckCode),
            _ => query.OrderByDescending(performance => performance.TotalTrips ?? 0).ThenBy(performance => performance.TruckCode)
        };
    }

    private static IQueryable<vw_DriverPerformance> ApplyDriverFilters(
        IQueryable<vw_DriverPerformance> query,
        DriverPerformanceSearchCriteria criteria)
    {
        if (criteria.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(performance =>
                EF.Functions.Like(performance.DriverCode, pattern, "\\") ||
                EF.Functions.Like(performance.DriverFullName, pattern, "\\"));
        }

        if (criteria.Status is { } status)
        {
            query = query.Where(performance => performance.DriverStatus == status);
        }

        return query;
    }

    private static IOrderedQueryable<vw_DriverPerformance> ApplyDriverSort(
        IQueryable<vw_DriverPerformance> query,
        string sort)
    {
        return sort switch
        {
            DriverPerformanceSort.TotalTonnageDesc => query.OrderByDescending(performance => performance.TotalTonnage ?? 0).ThenBy(performance => performance.DriverCode),
            DriverPerformanceSort.StopDurationDesc => query.OrderByDescending(performance => performance.TotalLinkedStopDurationMinutes ?? 0).ThenBy(performance => performance.DriverCode),
            DriverPerformanceSort.CompletionRateDesc => query.OrderByDescending(performance => performance.CompletionRatePercent ?? 0).ThenBy(performance => performance.DriverCode),
            DriverPerformanceSort.CodeAsc => query.OrderBy(performance => performance.DriverCode),
            _ => query.OrderByDescending(performance => performance.TotalTrips ?? 0).ThenBy(performance => performance.DriverCode)
        };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
