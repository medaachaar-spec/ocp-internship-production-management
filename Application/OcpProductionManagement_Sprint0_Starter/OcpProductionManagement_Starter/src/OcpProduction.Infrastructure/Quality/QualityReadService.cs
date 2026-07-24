using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Quality;

public sealed class QualityReadService : IQualityReadService
{
    private readonly OcpProductionDbContext _context;

    public QualityReadService(OcpProductionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<QualityResultListItemDto>> SearchAsync(
        QualitySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        IQueryable<vw_QualityAnalysis> query = _context.vw_QualityAnalyses.AsNoTracking();

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            var hasResultId = long.TryParse(search, out var resultId);

            query = query.Where(result =>
                (hasResultId && result.QualityResultID == resultId) ||
                (result.TruckCode != null && EF.Functions.Like(result.TruckCode, pattern, "\\")) ||
                (result.DriverCode != null && EF.Functions.Like(result.DriverCode, pattern, "\\")) ||
                (result.DriverFullName != null && EF.Functions.Like(result.DriverFullName, pattern, "\\")) ||
                (result.LoadingPointCode != null && EF.Functions.Like(result.LoadingPointCode, pattern, "\\")) ||
                (result.LoadingPointName != null && EF.Functions.Like(result.LoadingPointName, pattern, "\\")) ||
                (result.DestinationCode != null && EF.Functions.Like(result.DestinationCode, pattern, "\\")) ||
                (result.DestinationName != null && EF.Functions.Like(result.DestinationName, pattern, "\\")));
        }

        if (normalized.DateFrom is { } dateFrom)
        {
            var firstInstant = dateFrom.ToDateTime(TimeOnly.MinValue);
            query = query.Where(result => result.AnalysisDate >= firstInstant);
        }

        if (normalized.DateTo is { } dateTo)
        {
            if (dateTo == DateOnly.MaxValue)
            {
                var finalInstant = dateTo.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(result => result.AnalysisDate <= finalInstant);
            }
            else
            {
                var exclusiveEnd = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(result => result.AnalysisDate < exclusiveEnd);
            }
        }

        if (normalized.TripId is { } tripId)
        {
            query = query.Where(result => result.TripID == tripId);
        }

        if (normalized.TruckId is { } truckId)
        {
            query = query.Where(result => result.TruckID == truckId);
        }

        if (normalized.P2O5Min is { } minimum)
        {
            query = query.Where(result => result.P2O5Percent >= minimum);
        }

        if (normalized.P2O5Max is { } maximum)
        {
            query = query.Where(result => result.P2O5Percent <= maximum);
        }

        if (normalized.Status is { } status)
        {
            query = query.Where(result => result.QualityStatus == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderByDescending(result => result.AnalysisDate)
            .ThenByDescending(result => result.QualityResultID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(result => new QualityResultListItemDto
            {
                QualityResultId = result.QualityResultID,
                TripId = result.TripID,
                AnalysisDate = result.AnalysisDate,
                P2O5Value = result.P2O5Percent,
                QualityUnit = result.QualityUnit,
                TruckId = result.TruckID,
                TruckCode = result.TruckCode,
                LoadingPointCode = result.LoadingPointCode,
                LoadingPointName = result.LoadingPointName,
                DestinationCode = result.DestinationCode,
                DestinationName = result.DestinationName,
                Status = result.QualityStatus
            })
            .ToListAsync(cancellationToken);

        return PagedResult<QualityResultListItemDto>.Create(
            items,
            page,
            normalized.PageSize,
            totalItems);
    }

    public Task<QualityResultDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from result in _context.QualityResults.AsNoTracking()
            join analysis in _context.vw_QualityAnalyses.AsNoTracking()
                on result.QualityResultID equals analysis.QualityResultID
            where result.QualityResultID == id
            select new QualityResultDetailsDto
            {
                QualityResultId = result.QualityResultID,
                TripId = result.TripID,
                AnalysisDate = result.AnalysisDate,
                QualityValue = result.QualityValue,
                QualityUnit = result.QualityUnit,
                TargetValue = result.TargetValue,
                Status = result.Status,
                Comments = result.Comments,
                CreatedAt = result.CreatedAt,
                TruckId = analysis.TruckID,
                TruckCode = analysis.TruckCode,
                DriverId = analysis.DriverID,
                DriverCode = analysis.DriverCode,
                DriverDisplayName = analysis.DriverFullName,
                WorkShiftId = analysis.ShiftID,
                WorkShiftCode = analysis.ShiftCode,
                WorkShiftName = analysis.ShiftName,
                LoadingPointId = result.LoadingPointID,
                LoadingPointCode = analysis.LoadingPointCode,
                LoadingPointName = analysis.LoadingPointName,
                DestinationId = analysis.DestinationID,
                DestinationCode = analysis.DestinationCode,
                DestinationName = analysis.DestinationName
            };

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<QualityFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trucks = await _context.vw_QualityAnalyses
            .AsNoTracking()
            .Where(result => result.TruckID != null && result.TruckCode != null)
            .Select(result => new { result.TruckID, result.TruckCode })
            .Distinct()
            .OrderBy(truck => truck.TruckCode)
            .ToListAsync(cancellationToken);

        var statuses = await _context.QualityResults
            .AsNoTracking()
            .Select(result => result.Status)
            .Distinct()
            .OrderBy(status => status)
            .ToListAsync(cancellationToken);

        return new QualityFilterOptions
        {
            Trucks = trucks
                .Select(truck => new QualityFilterOption(truck.TruckID!.Value, truck.TruckCode!))
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
