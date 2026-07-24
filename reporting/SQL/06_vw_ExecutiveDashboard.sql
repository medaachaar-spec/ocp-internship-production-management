/*
    View: dbo.vw_ExecutiveDashboard
    Objective: provide one global KPI row for the Power BI executive dashboard.
    Grain: exactly one row for the complete prototype.
    Sources: reporting detail views and reference tables.
    Rules: transactional sources are aggregated independently; all divisions
           use NULLIF; counters and sums return zero when no rows exist.
    Dependencies: dbo.vw_TripSummary, dbo.vw_StopAnalysis,
                  dbo.vw_QualityAnalysis.
    Assumptions: reference IsActive flags define active trucks and drivers;
                 quality coverage includes every quality unit linked to a trip.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_ExecutiveDashboard
AS
WITH TripMetrics AS (
    SELECT
        MIN(trip.TripDate) AS DataStartDate,
        MAX(trip.TripDate) AS DataEndDate,
        CASE
            WHEN COUNT_BIG(*) = 0 THEN CONVERT(INT, NULL)
            ELSE DATEDIFF(DAY, MIN(trip.TripDate), MAX(trip.TripDate)) + 1
        END AS TotalCalendarDays,
        COUNT_BIG(DISTINCT trip.TripDate) AS TotalActiveTripDays,
        COUNT_BIG(*) AS TotalTrips,
        COALESCE(SUM(CONVERT(BIGINT, trip.IsCompleted)), CONVERT(BIGINT, 0)) AS CompletedTrips,
        COALESCE(SUM(CONVERT(BIGINT, trip.IsIncomplete)), CONVERT(BIGINT, 0)) AS IncompleteTrips,
        CONVERT(DECIMAL(18,2), COALESCE(SUM(COALESCE(trip.Tonnage, 0)), 0)) AS TotalTonnage,
        CONVERT(DECIMAL(18,2), AVG(CONVERT(DECIMAL(18,2), trip.Tonnage))) AS AveragePayloadTonnes,
        CONVERT(
            DECIMAL(10,2),
            AVG(CONVERT(DECIMAL(18,2), trip.PayloadUtilizationPercent))
        ) AS AveragePayloadUtilizationPercent,
        CONVERT(
            DECIMAL(18,2),
            AVG(CONVERT(DECIMAL(18,2), trip.TripDurationMinutes))
        ) AS AverageTripDurationMinutes,
        MIN(trip.TripDurationMinutes) AS MinimumTripDurationMinutes,
        MAX(trip.TripDurationMinutes) AS MaximumTripDurationMinutes
    FROM dbo.vw_TripSummary AS trip
),
StopMetrics AS (
    SELECT
        COUNT_BIG(*) AS TotalStops,
        COALESCE(SUM(CONVERT(BIGINT, stop.HasLinkedTrip)), CONVERT(BIGINT, 0)) AS StopsLinkedToTrips,
        COALESCE(SUM(CONVERT(BIGINT, CASE WHEN stop.HasLinkedTrip = 0 THEN 1 ELSE 0 END)), CONVERT(BIGINT, 0)) AS StopsWithoutTrips,
        COALESCE(SUM(CONVERT(BIGINT, stop.IsCompletedStop)), CONVERT(BIGINT, 0)) AS CompletedStops,
        COALESCE(SUM(CONVERT(BIGINT, stop.IsOpenStop)), CONVERT(BIGINT, 0)) AS OpenStops,
        COALESCE(SUM(CONVERT(BIGINT, COALESCE(stop.StopDurationMinutes, 0))), CONVERT(BIGINT, 0)) AS TotalDowntimeMinutes,
        CONVERT(
            DECIMAL(18,2),
            AVG(CONVERT(DECIMAL(18,2), stop.StopDurationMinutes))
        ) AS AverageStopDurationMinutes,
        MAX(stop.StopDurationMinutes) AS MaximumStopDurationMinutes
    FROM dbo.vw_StopAnalysis AS stop
),
QualityMetrics AS (
    SELECT
        COUNT_BIG(*) AS TotalQualityResults,
        COALESCE(SUM(CONVERT(BIGINT, quality.HasLinkedTrip)), CONVERT(BIGINT, 0)) AS LinkedQualityResults,
        COALESCE(SUM(CONVERT(BIGINT, CASE WHEN quality.HasLinkedTrip = 0 THEN 1 ELSE 0 END)), CONVERT(BIGINT, 0)) AS UnlinkedQualityResults,
        COALESCE(SUM(CONVERT(BIGINT, quality.IsP2O5Measurement)), CONVERT(BIGINT, 0)) AS TotalP2O5Results,
        CONVERT(
            DECIMAL(10,3),
            AVG(CONVERT(DECIMAL(18,3), quality.P2O5Percent))
        ) AS AverageP2O5,
        MIN(quality.P2O5Percent) AS MinimumP2O5,
        MAX(quality.P2O5Percent) AS MaximumP2O5
    FROM dbo.vw_QualityAnalysis AS quality
),
ReferenceMetrics AS (
    SELECT
        (SELECT COUNT_BIG(*) FROM dbo.Truck) AS TotalTrucks,
        (SELECT COUNT_BIG(*) FROM dbo.Truck WHERE IsActive = 1) AS ActiveTrucks,
        (SELECT COUNT_BIG(*) FROM dbo.Driver) AS TotalDrivers,
        (SELECT COUNT_BIG(*) FROM dbo.Driver WHERE IsActive = 1) AS ActiveDrivers,
        (SELECT COUNT_BIG(*) FROM dbo.LoadingPoint) AS TotalLoadingPoints,
        (SELECT COUNT_BIG(*) FROM dbo.Destination) AS TotalDestinations
),
QualityTripCoverage AS (
    SELECT quality.TripID
    FROM dbo.vw_QualityAnalysis AS quality
    WHERE quality.TripID IS NOT NULL
    GROUP BY quality.TripID
),
CoverageMetrics AS (
    SELECT
        COALESCE(SUM(CONVERT(BIGINT, trip.HasStops)), CONVERT(BIGINT, 0)) AS TripsWithStops,
        COALESCE(SUM(CONVERT(BIGINT, CASE WHEN trip.HasStops = 0 THEN 1 ELSE 0 END)), CONVERT(BIGINT, 0)) AS TripsWithoutStops,
        COALESCE(SUM(CONVERT(BIGINT, CASE WHEN qualityTrip.TripID IS NOT NULL THEN 1 ELSE 0 END)), CONVERT(BIGINT, 0)) AS TripsWithQualityResults,
        COALESCE(SUM(CONVERT(BIGINT, CASE WHEN qualityTrip.TripID IS NULL THEN 1 ELSE 0 END)), CONVERT(BIGINT, 0)) AS TripsWithoutQualityResults,
        COUNT_BIG(DISTINCT trip.TruckID) AS TrucksWithTrips,
        COUNT_BIG(DISTINCT trip.DriverID) AS DriversWithTrips
    FROM dbo.vw_TripSummary AS trip
    LEFT JOIN QualityTripCoverage AS qualityTrip
        ON qualityTrip.TripID = trip.TripID
)
SELECT
    tripMetrics.DataStartDate,
    tripMetrics.DataEndDate,
    tripMetrics.TotalCalendarDays,
    tripMetrics.TotalActiveTripDays,

    referenceMetrics.TotalTrucks,
    referenceMetrics.ActiveTrucks,
    referenceMetrics.TotalDrivers,
    referenceMetrics.ActiveDrivers,
    referenceMetrics.TotalLoadingPoints,
    referenceMetrics.TotalDestinations,

    tripMetrics.TotalTrips,
    tripMetrics.CompletedTrips,
    tripMetrics.IncompleteTrips,
    CONVERT(
        DECIMAL(10,2),
        CONVERT(DECIMAL(19,4), tripMetrics.CompletedTrips) * CONVERT(DECIMAL(5,2), 100.00)
            / NULLIF(CONVERT(DECIMAL(19,4), tripMetrics.TotalTrips), 0)
    ) AS CompletionRatePercent,
    tripMetrics.TotalTonnage,
    tripMetrics.AveragePayloadTonnes,
    tripMetrics.AveragePayloadUtilizationPercent,
    tripMetrics.AverageTripDurationMinutes,
    tripMetrics.MinimumTripDurationMinutes,
    tripMetrics.MaximumTripDurationMinutes,

    stopMetrics.TotalStops,
    stopMetrics.StopsLinkedToTrips,
    stopMetrics.StopsWithoutTrips,
    stopMetrics.CompletedStops,
    stopMetrics.OpenStops,
    stopMetrics.TotalDowntimeMinutes,
    stopMetrics.AverageStopDurationMinutes,
    stopMetrics.MaximumStopDurationMinutes,

    qualityMetrics.TotalQualityResults,
    qualityMetrics.LinkedQualityResults,
    qualityMetrics.UnlinkedQualityResults,
    qualityMetrics.TotalP2O5Results,
    qualityMetrics.AverageP2O5,
    qualityMetrics.MinimumP2O5,
    qualityMetrics.MaximumP2O5,

    coverageMetrics.TripsWithStops,
    coverageMetrics.TripsWithoutStops,
    coverageMetrics.TripsWithQualityResults,
    coverageMetrics.TripsWithoutQualityResults,
    coverageMetrics.TrucksWithTrips,
    coverageMetrics.DriversWithTrips,

    CONVERT(
        DECIMAL(18,2),
        CONVERT(DECIMAL(19,4), tripMetrics.TotalTrips)
            / NULLIF(CONVERT(DECIMAL(19,4), tripMetrics.TotalActiveTripDays), 0)
    ) AS AverageTripsPerActiveDay,
    CONVERT(
        DECIMAL(18,2),
        CONVERT(DECIMAL(38,4), tripMetrics.TotalTonnage)
            / NULLIF(CONVERT(DECIMAL(19,4), tripMetrics.TotalActiveTripDays), 0)
    ) AS AverageTonnagePerActiveDay
FROM TripMetrics AS tripMetrics
CROSS JOIN StopMetrics AS stopMetrics
CROSS JOIN QualityMetrics AS qualityMetrics
CROSS JOIN ReferenceMetrics AS referenceMetrics
CROSS JOIN CoverageMetrics AS coverageMetrics;
GO
