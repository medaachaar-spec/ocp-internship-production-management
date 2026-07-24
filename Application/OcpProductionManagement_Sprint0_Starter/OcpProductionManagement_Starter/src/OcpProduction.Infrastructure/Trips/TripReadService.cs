using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Trips;

public sealed class TripReadService : ITripReadService
{
    private readonly OcpProductionDbContext _context;

    public TripReadService(OcpProductionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TripListItemDto>> SearchAsync(
        TripSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        IQueryable<vw_TripSummary> query = _context.vw_TripSummaries.AsNoTracking();

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            var hasTripId = long.TryParse(search, out var tripId);

            query = query.Where(trip =>
                (hasTripId && trip.TripID == tripId) ||
                EF.Functions.Like(trip.TruckCode, pattern, "\\") ||
                EF.Functions.Like(trip.DriverCode, pattern, "\\") ||
                EF.Functions.Like(trip.FullName, pattern, "\\") ||
                EF.Functions.Like(trip.LoadingPointCode, pattern, "\\") ||
                EF.Functions.Like(trip.LoadingPointName, pattern, "\\") ||
                EF.Functions.Like(trip.DestinationCode, pattern, "\\") ||
                EF.Functions.Like(trip.DestinationName, pattern, "\\"));
        }

        if (normalized.DateFrom is { } dateFrom)
        {
            query = query.Where(trip => trip.TripDate >= dateFrom);
        }

        if (normalized.DateTo is { } dateTo)
        {
            query = query.Where(trip => trip.TripDate <= dateTo);
        }

        if (normalized.TruckId is { } truckId)
        {
            query = query.Where(trip => trip.TruckID == truckId);
        }

        if (normalized.DriverId is { } driverId)
        {
            query = query.Where(trip => trip.DriverID == driverId);
        }

        if (normalized.WorkShiftId is { } workShiftId)
        {
            query = query.Where(trip => trip.ShiftID == workShiftId);
        }

        if (normalized.Status is { } status)
        {
            query = query.Where(trip => trip.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderByDescending(trip => trip.StartTime)
            .ThenByDescending(trip => trip.TripID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(trip => new TripListItemDto
            {
                TripId = trip.TripID,
                TripDate = trip.TripDate,
                TruckId = trip.TruckID,
                TruckCode = trip.TruckCode,
                DriverId = trip.DriverID,
                DriverCode = trip.DriverCode,
                DriverDisplayName = trip.FullName,
                WorkShiftId = trip.ShiftID,
                WorkShiftCode = trip.ShiftCode,
                WorkShiftName = trip.ShiftName,
                LoadingPointId = trip.LoadingPointID,
                LoadingPointName = trip.LoadingPointName,
                DestinationId = trip.DestinationID,
                DestinationName = trip.DestinationName,
                StartDateTime = trip.StartTime,
                EndDateTime = trip.EndTime,
                DurationMinutes = trip.TripDurationMinutes,
                Tonnage = trip.Tonnage,
                Status = trip.Status
            })
            .ToListAsync(cancellationToken);

        return PagedResult<TripListItemDto>.Create(
            items,
            page,
            normalized.PageSize,
            totalItems);
    }

    public Task<TripDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from detail in _context.vw_TripDetails.AsNoTracking()
            join summary in _context.vw_TripSummaries.AsNoTracking()
                on detail.TripID equals summary.TripID
            join trip in _context.Trips.AsNoTracking()
                on detail.TripID equals trip.TripID
            where detail.TripID == id
            select new TripDetailsDto
            {
                TripId = detail.TripID,
                TripDate = detail.TripDate,
                TruckId = detail.TruckID,
                TruckCode = detail.TruckCode,
                RegistrationNumber = detail.RegistrationNumber,
                DriverId = detail.DriverID,
                DriverCode = detail.DriverCode,
                DriverDisplayName = detail.DriverName,
                WorkShiftId = detail.ShiftID,
                WorkShiftCode = detail.ShiftCode,
                WorkShiftName = detail.ShiftName,
                LoadingPointId = detail.LoadingPointID,
                LoadingPointCode = detail.LoadingPointCode,
                LoadingPointName = detail.LoadingPointName,
                LoadingZoneName = detail.ZoneName,
                DestinationId = detail.DestinationID,
                DestinationCode = detail.DestinationCode,
                DestinationName = detail.DestinationName,
                DestinationType = detail.DestinationType,
                StartDateTime = detail.StartTime,
                EndDateTime = detail.EndTime,
                DurationMinutes = detail.DurationMinutes,
                IsLongTrip = detail.IsLongTrip == 1,
                Tonnage = detail.Tonnage,
                Status = detail.Status,
                Comments = detail.Comments,
                CreatedAt = trip.CreatedAt,
                StopCount = summary.StopCount ?? 0,
                TotalStopDurationMinutes = summary.TotalStopDurationMinutes ?? 0,
                QualityResultCount = summary.QualityResultCount ?? 0,
                LatestP2O5 = summary.LatestP2O5
            };

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TripFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trucks = await _context.vw_TripSummaries
            .AsNoTracking()
            .Select(trip => new { trip.TruckID, trip.TruckCode })
            .Distinct()
            .OrderBy(truck => truck.TruckCode)
            .ToListAsync(cancellationToken);

        var drivers = await _context.vw_TripSummaries
            .AsNoTracking()
            .Select(trip => new { trip.DriverID, trip.DriverCode, trip.FullName })
            .Distinct()
            .OrderBy(driver => driver.DriverCode)
            .ToListAsync(cancellationToken);

        var workShifts = await _context.vw_TripSummaries
            .AsNoTracking()
            .Select(trip => new { trip.ShiftID, trip.ShiftCode, trip.ShiftName })
            .Distinct()
            .OrderBy(shift => shift.ShiftCode)
            .ToListAsync(cancellationToken);

        var statuses = await _context.vw_TripSummaries
            .AsNoTracking()
            .Select(trip => trip.Status)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);

        return new TripFilterOptions
        {
            Trucks = trucks
                .Select(truck => new TripFilterOption(truck.TruckID, truck.TruckCode))
                .ToArray(),
            Drivers = drivers
                .Select(driver => new TripFilterOption(
                    driver.DriverID,
                    $"{driver.DriverCode} - {driver.FullName}"))
                .ToArray(),
            WorkShifts = workShifts
                .Select(shift => new TripFilterOption(
                    shift.ShiftID,
                    $"{shift.ShiftName} ({shift.ShiftCode})"))
                .ToArray(),
            Statuses = statuses
        };
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
