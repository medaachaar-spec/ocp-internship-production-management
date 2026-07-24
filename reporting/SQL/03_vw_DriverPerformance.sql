/*
    View: dbo.vw_DriverPerformance
    Objective: expose Power BI performance KPIs for every driver.
    Grain: one row per dbo.Driver row.
    Sources: dbo.Driver, dbo.vw_TripSummary, dbo.TruckStop,
             dbo.Trip, dbo.QualityResult.
    Rules: only stops explicitly linked through TruckStop.TripID are attributed;
           quality count includes all units and P2O5 statistics are conditional.
    Dependencies: dbo.vw_TripSummary must already exist.
    Assumptions: Trip.DriverID is authoritative for linked-stop attribution;
                 IsActive is the authoritative active-reference flag.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_DriverPerformance
AS
WITH TripMetrics AS (
    SELECT
        trip.DriverID,
        COUNT_BIG(*) AS TotalTrips,
        SUM(CONVERT(BIGINT, trip.IsCompleted)) AS CompletedTrips,
        SUM(CONVERT(BIGINT, trip.IsIncomplete)) AS IncompleteTrips,
        COUNT_BIG(DISTINCT trip.TripDate) AS ActiveTripDays,
        MIN(trip.TripDate) AS FirstTripDate,
        MAX(trip.TripDate) AS LastTripDate,
        CONVERT(DECIMAL(18,2), SUM(COALESCE(trip.Tonnage, 0))) AS TotalTonnage,
        CONVERT(DECIMAL(18,2), AVG(CONVERT(DECIMAL(18,2), trip.Tonnage))) AS AveragePayloadTonnes,
        MIN(trip.Tonnage) AS MinimumPayloadTonnes,
        MAX(trip.Tonnage) AS MaximumPayloadTonnes,
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
    GROUP BY trip.DriverID
),
LinkedStopMetrics AS (
    SELECT
        trip.DriverID,
        COUNT_BIG(DISTINCT stop.TripID) AS TripsWithStops,
        COUNT_BIG(*) AS TotalLinkedStopCount,
        SUM(CONVERT(BIGINT, COALESCE(stop.DurationMinutes, 0))) AS TotalLinkedStopDurationMinutes,
        CONVERT(
            DECIMAL(18,2),
            AVG(
                CASE
                    WHEN stop.DurationMinutes >= 0
                        THEN CONVERT(DECIMAL(18,2), stop.DurationMinutes)
                    ELSE NULL
                END
            )
        ) AS AverageLinkedStopDurationMinutes,
        MAX(CASE WHEN stop.DurationMinutes >= 0 THEN stop.DurationMinutes ELSE NULL END) AS MaximumLinkedStopDurationMinutes
    FROM dbo.TruckStop AS stop
    INNER JOIN dbo.Trip AS trip
        ON trip.TripID = stop.TripID
    GROUP BY trip.DriverID
),
QualityMetrics AS (
    SELECT
        trip.DriverID,
        COUNT_BIG(*) AS QualityResultCount,
        CONVERT(
            DECIMAL(10,3),
            AVG(
                CASE
                    WHEN quality.QualityUnit = N'P2O5Percent'
                        THEN CONVERT(DECIMAL(18,3), quality.QualityValue)
                    ELSE NULL
                END
            )
        ) AS AverageP2O5,
        MIN(CASE WHEN quality.QualityUnit = N'P2O5Percent' THEN quality.QualityValue ELSE NULL END) AS MinimumP2O5,
        MAX(CASE WHEN quality.QualityUnit = N'P2O5Percent' THEN quality.QualityValue ELSE NULL END) AS MaximumP2O5
    FROM dbo.QualityResult AS quality
    INNER JOIN dbo.Trip AS trip
        ON trip.TripID = quality.TripID
    GROUP BY trip.DriverID
)
SELECT
    driver.DriverID,
    driver.DriverCode,
    driver.FullName AS DriverFullName,
    driver.Status AS DriverStatus,

    COALESCE(tripMetrics.TotalTrips, CONVERT(BIGINT, 0)) AS TotalTrips,
    COALESCE(tripMetrics.CompletedTrips, CONVERT(BIGINT, 0)) AS CompletedTrips,
    COALESCE(tripMetrics.IncompleteTrips, CONVERT(BIGINT, 0)) AS IncompleteTrips,
    CONVERT(
        DECIMAL(10,2),
        CONVERT(DECIMAL(19,4), tripMetrics.CompletedTrips) * CONVERT(DECIMAL(5,2), 100.00)
            / NULLIF(CONVERT(DECIMAL(19,4), tripMetrics.TotalTrips), 0)
    ) AS CompletionRatePercent,
    COALESCE(tripMetrics.ActiveTripDays, CONVERT(BIGINT, 0)) AS ActiveTripDays,
    tripMetrics.FirstTripDate,
    tripMetrics.LastTripDate,
    CONVERT(
        DECIMAL(18,2),
        CONVERT(DECIMAL(19,4), tripMetrics.TotalTrips)
            / NULLIF(CONVERT(DECIMAL(19,4), tripMetrics.ActiveTripDays), 0)
    ) AS TripsPerActiveDay,
    CONVERT(BIT, CASE WHEN tripMetrics.TotalTrips > 0 THEN 1 ELSE 0 END) AS HasTrips,

    COALESCE(tripMetrics.TotalTonnage, CONVERT(DECIMAL(18,2), 0)) AS TotalTonnage,
    tripMetrics.AveragePayloadTonnes,
    tripMetrics.MinimumPayloadTonnes,
    tripMetrics.MaximumPayloadTonnes,
    tripMetrics.AveragePayloadUtilizationPercent,
    tripMetrics.AverageTripDurationMinutes,
    tripMetrics.MinimumTripDurationMinutes,
    tripMetrics.MaximumTripDurationMinutes,

    COALESCE(linkedStops.TripsWithStops, CONVERT(BIGINT, 0)) AS TripsWithStops,
    COALESCE(linkedStops.TotalLinkedStopCount, CONVERT(BIGINT, 0)) AS TotalLinkedStopCount,
    COALESCE(linkedStops.TotalLinkedStopDurationMinutes, CONVERT(BIGINT, 0)) AS TotalLinkedStopDurationMinutes,
    linkedStops.AverageLinkedStopDurationMinutes,
    linkedStops.MaximumLinkedStopDurationMinutes,
    CONVERT(BIT, CASE WHEN linkedStops.TotalLinkedStopCount > 0 THEN 1 ELSE 0 END) AS HasLinkedStops,

    COALESCE(qualityMetrics.QualityResultCount, CONVERT(BIGINT, 0)) AS QualityResultCount,
    qualityMetrics.AverageP2O5,
    qualityMetrics.MinimumP2O5,
    qualityMetrics.MaximumP2O5,
    CONVERT(BIT, CASE WHEN qualityMetrics.QualityResultCount > 0 THEN 1 ELSE 0 END) AS HasQualityResults
FROM dbo.Driver AS driver
LEFT JOIN TripMetrics AS tripMetrics
    ON tripMetrics.DriverID = driver.DriverID
LEFT JOIN LinkedStopMetrics AS linkedStops
    ON linkedStops.DriverID = driver.DriverID
LEFT JOIN QualityMetrics AS qualityMetrics
    ON qualityMetrics.DriverID = driver.DriverID;
GO
