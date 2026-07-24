using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Application.Dashboard.Services;
using OcpProduction.Application.Trips;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Dashboard;

public sealed class DashboardReadService(OcpProductionDbContext context) : IDashboardReadService
{
    public async Task<DashboardDto> GetDashboardAsync(
        DashboardDateCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var trips = ApplyTripDateFilter(
            context.vw_TripSummaries.AsNoTracking(),
            criteria);
        var stops = ApplyStopDateFilter(
            context.vw_StopAnalyses.AsNoTracking(),
            criteria);
        var qualityResults = ApplyQualityDateFilter(
            context.vw_QualityAnalyses.AsNoTracking(),
            criteria);

        var dailyTripsAndTonnage = await trips
            .GroupBy(trip => trip.TripDate)
            .Select(group => new DailyTripTonnagePointDto
            {
                Date = group.Key,
                TripCount = group
                    .Select(trip => trip.TripID)
                    .Distinct()
                    .LongCount(),
                CompletedTonnage = group
                    .Where(trip => trip.Status == TripStatuses.Completed)
                    .Sum(trip => trip.Tonnage ?? 0m)
            })
            .OrderBy(point => point.Date)
            .ToListAsync(cancellationToken);

        var tripsByStatus = await trips
            .GroupBy(trip => trip.Status)
            .Select(group => new TripStatusPointDto
            {
                Status = group.Key,
                TripCount = group
                    .Select(trip => trip.TripID)
                    .Distinct()
                    .LongCount()
            })
            .OrderBy(point => point.Status)
            .ToListAsync(cancellationToken);

        var truckActivity = await trips
            .GroupBy(trip => new { trip.TruckID, trip.TruckCode })
            .Select(group => new TruckActivityPointDto
            {
                TruckId = group.Key.TruckID,
                TruckCode = group.Key.TruckCode,
                TripCount = group
                    .Select(trip => trip.TripID)
                    .Distinct()
                    .LongCount()
            })
            .OrderByDescending(point => point.TripCount)
            .ThenBy(point => point.TruckCode)
            .ThenBy(point => point.TruckId)
            .ToListAsync(cancellationToken);

        var stopDurationByReason = await stops
            .GroupBy(stop => new { stop.StopReasonID, stop.StopReasonName })
            .Select(group => new StopReasonDurationPointDto
            {
                StopReasonId = group.Key.StopReasonID,
                StopReasonName = group.Key.StopReasonName,
                StopCount = group
                    .Select(stop => stop.TruckStopID)
                    .Distinct()
                    .LongCount(),
                DurationMinutes = group.Sum(
                    stop => Convert.ToInt64(stop.StopDurationMinutes ?? 0))
            })
            .OrderByDescending(point => point.DurationMinutes)
            .ThenBy(point => point.StopReasonName)
            .ThenBy(point => point.StopReasonId)
            .ToListAsync(cancellationToken);

        var qualityByDate = await qualityResults
            .GroupBy(result => result.QualityDate)
            .Select(group => new
            {
                Date = group.Key,
                AnalysisCount = group
                    .Select(result => result.QualityResultID)
                    .Distinct()
                    .LongCount(),
                MeasurementCount = group.LongCount(
                    result => result.P2O5Percent.HasValue),
                AverageP2O5 = group.Average(
                    result => result.P2O5Percent)
            })
            .OrderBy(point => point.Date)
            .ToListAsync(cancellationToken);

        var p2o5Trend = qualityByDate
            .Where(point =>
                point.Date.HasValue &&
                point.AverageP2O5.HasValue)
            .Select(point => new P2O5TrendPointDto
            {
                Date = point.Date!.Value,
                AverageP2O5 = point.AverageP2O5!.Value,
                MeasurementCount = point.MeasurementCount
            })
            .ToList();

        var tripCount = dailyTripsAndTonnage.Sum(point => point.TripCount);
        var totalTonnage = dailyTripsAndTonnage.Sum(point => point.CompletedTonnage);
        var pendingValidationTripCount = tripsByStatus
            .Where(point => point.Status == TripStatuses.PendingValidation)
            .Sum(point => point.TripCount);
        var stopCount = stopDurationByReason.Sum(point => point.StopCount);
        var totalStopDurationMinutes = stopDurationByReason.Sum(
            point => point.DurationMinutes);
        var qualityAnalysisCount = qualityByDate.Sum(point => point.AnalysisCount);

        return new DashboardDto
        {
            TripCount = tripCount,
            TotalTonnage = totalTonnage,
            StopCount = stopCount,
            TotalStopDurationHours = totalStopDurationMinutes / 60m,
            QualityAnalysisCount = qualityAnalysisCount,
            PendingValidationTripCount = pendingValidationTripCount,
            Charts = new DashboardChartDataDto
            {
                DailyTripsAndTonnage = dailyTripsAndTonnage,
                TripsByStatus = tripsByStatus,
                StopDurationByReason = stopDurationByReason,
                TruckActivity = truckActivity,
                P2O5Trend = p2o5Trend
            }
        };
    }

    private static IQueryable<vw_TripSummary> ApplyTripDateFilter(
        IQueryable<vw_TripSummary> query,
        DashboardDateCriteria criteria)
    {
        if (criteria.StartDate is { } startDate)
        {
            query = query.Where(trip => trip.TripDate >= startDate);
        }

        if (criteria.EndDate is { } endDate)
        {
            query = query.Where(trip => trip.TripDate <= endDate);
        }

        return query;
    }

    private static IQueryable<vw_StopAnalysis> ApplyStopDateFilter(
        IQueryable<vw_StopAnalysis> query,
        DashboardDateCriteria criteria)
    {
        if (criteria.StartDate is { } startDate)
        {
            query = query.Where(stop =>
                stop.StopDate.HasValue &&
                stop.StopDate.Value >= startDate);
        }

        if (criteria.EndDate is { } endDate)
        {
            query = query.Where(stop =>
                stop.StopDate.HasValue &&
                stop.StopDate.Value <= endDate);
        }

        return query;
    }

    private static IQueryable<vw_QualityAnalysis> ApplyQualityDateFilter(
        IQueryable<vw_QualityAnalysis> query,
        DashboardDateCriteria criteria)
    {
        if (criteria.StartDate is { } startDate)
        {
            query = query.Where(result =>
                result.QualityDate.HasValue &&
                result.QualityDate.Value >= startDate);
        }

        if (criteria.EndDate is { } endDate)
        {
            query = query.Where(result =>
                result.QualityDate.HasValue &&
                result.QualityDate.Value <= endDate);
        }

        return query;
    }
}
