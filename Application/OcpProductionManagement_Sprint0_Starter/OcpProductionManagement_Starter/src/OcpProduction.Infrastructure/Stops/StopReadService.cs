using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Stops;

public sealed class StopReadService : IStopReadService
{
    private readonly OcpProductionDbContext _context;

    public StopReadService(OcpProductionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StopListItemDto>> SearchAsync(
        StopSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        IQueryable<vw_TruckStopDetail> query = _context.vw_TruckStopDetails.AsNoTracking();

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            var hasIdentifier = long.TryParse(search, out var identifier);

            query = query.Where(stop =>
                (hasIdentifier &&
                    (stop.TruckStopID == identifier || stop.TripID == identifier)) ||
                EF.Functions.Like(stop.TruckCode, pattern, "\\") ||
                EF.Functions.Like(stop.ReasonCode, pattern, "\\") ||
                EF.Functions.Like(stop.ReasonName, pattern, "\\") ||
                (stop.Comments != null && EF.Functions.Like(stop.Comments, pattern, "\\")));
        }

        if (normalized.DateFrom is { } dateFrom)
        {
            var lowerBound = dateFrom.ToDateTime(TimeOnly.MinValue);
            query = query.Where(stop => stop.StartTime >= lowerBound);
        }

        if (normalized.DateTo is { } dateTo)
        {
            if (dateTo == DateOnly.MaxValue)
            {
                query = query.Where(stop => stop.StartTime <= DateTime.MaxValue);
            }
            else
            {
                var exclusiveUpperBound = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(stop => stop.StartTime < exclusiveUpperBound);
            }
        }

        if (normalized.TruckId is { } truckId)
        {
            query = query.Where(stop => stop.TruckID == truckId);
        }

        if (normalized.StopReasonId is { } stopReasonId)
        {
            query = query.Where(stop => stop.StopReasonID == stopReasonId);
        }

        if (normalized.TripId is { } tripId)
        {
            query = query.Where(stop => stop.TripID == tripId);
        }

        if (normalized.State == StopStates.Open)
        {
            query = query.Where(stop => stop.EndTime == null);
        }
        else if (normalized.State == StopStates.Closed)
        {
            query = query.Where(stop => stop.EndTime != null);
        }

        if (normalized.ValidationStatus is { } validationStatus)
        {
            query = query.Where(stop => stop.ValidationStatus == validationStatus);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderByDescending(stop => stop.StartTime)
            .ThenByDescending(stop => stop.TruckStopID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(stop => new StopListItemDto
            {
                StopId = stop.TruckStopID,
                TripId = stop.TripID,
                TruckId = stop.TruckID,
                TruckCode = stop.TruckCode,
                StopReasonId = stop.StopReasonID,
                StopReasonCode = stop.ReasonCode,
                StopReasonName = stop.ReasonName,
                StartTime = stop.StartTime,
                EndTime = stop.EndTime,
                DurationMinutes = stop.DurationMinutes,
                State = stop.EndTime == null ? StopStates.Open : StopStates.Closed,
                ValidationStatus = stop.ValidationStatus
            })
            .ToListAsync(cancellationToken);

        return PagedResult<StopListItemDto>.Create(
            items,
            page,
            normalized.PageSize,
            totalItems);
    }

    public Task<StopDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _context.TruckStops
            .AsNoTracking()
            .Where(stop => stop.TruckStopID == id)
            .Select(stop => new StopDetailsDto
            {
                StopId = stop.TruckStopID,
                TripId = stop.TripID,
                TruckId = stop.TruckID,
                TruckCode = stop.Truck.TruckCode,
                TruckRegistrationNumber = stop.Truck.RegistrationNumber,
                TruckModel = stop.Truck.Model,
                DriverId = stop.DriverID,
                DriverCode = stop.Driver == null ? null : stop.Driver.DriverCode,
                DriverName = stop.Driver == null ? null : stop.Driver.FullName,
                WorkShiftId = stop.ShiftID,
                WorkShiftCode = stop.Shift == null ? null : stop.Shift.ShiftCode,
                WorkShiftName = stop.Shift == null ? null : stop.Shift.ShiftName,
                StopReasonId = stop.StopReasonID,
                StopReasonCode = stop.StopReason.ReasonCode,
                StopReasonName = stop.StopReason.ReasonName,
                StopReasonCategory = stop.StopReason.Category,
                IsPlanned = stop.StopReason.IsPlanned,
                StartTime = stop.StartTime,
                EndTime = stop.EndTime,
                DurationMinutes = stop.DurationMinutes,
                State = stop.EndTime == null ? StopStates.Open : StopStates.Closed,
                ValidationStatus = stop.ValidationStatus,
                Observation = stop.Comments,
                CreatedAt = stop.CreatedAt,
                LoadingPointCode = stop.Trip == null ? null : stop.Trip.LoadingPoint.LoadingPointCode,
                LoadingPointName = stop.Trip == null ? null : stop.Trip.LoadingPoint.LoadingPointName,
                DestinationCode = stop.Trip == null ? null : stop.Trip.Destination.DestinationCode,
                DestinationName = stop.Trip == null ? null : stop.Trip.Destination.DestinationName
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StopFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trucks = await _context.vw_TruckStopDetails
            .AsNoTracking()
            .Select(stop => new { stop.TruckID, stop.TruckCode })
            .Distinct()
            .OrderBy(truck => truck.TruckCode)
            .ToListAsync(cancellationToken);

        var reasons = await _context.vw_TruckStopDetails
            .AsNoTracking()
            .Select(stop => new { stop.StopReasonID, stop.ReasonCode, stop.ReasonName })
            .Distinct()
            .OrderBy(reason => reason.ReasonName)
            .ToListAsync(cancellationToken);

        var validationStatuses = await _context.vw_TruckStopDetails
            .AsNoTracking()
            .Select(stop => stop.ValidationStatus)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);

        return new StopFilterOptions
        {
            Trucks = trucks
                .Select(truck => new StopFilterOption(truck.TruckID, truck.TruckCode))
                .ToArray(),
            StopReasons = reasons
                .Select(reason => new StopFilterOption(
                    reason.StopReasonID,
                    $"{reason.ReasonName} ({reason.ReasonCode})"))
                .ToArray(),
            ValidationStatuses = validationStatuses
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
