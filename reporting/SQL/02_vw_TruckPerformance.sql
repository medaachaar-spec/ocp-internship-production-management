/*
    View: dbo.vw_TruckPerformance
    Objective: expose Power BI performance KPIs for every truck.
    Grain: one row per dbo.Truck row.
    Sources: dbo.Truck, dbo.vw_TripSummary, dbo.TruckStop,
             dbo.QualityResult, dbo.Trip.
    Rules: trips, stops, and quality are aggregated independently before joins;
           quality count includes all units and P2O5 statistics are conditional.
    Dependencies: dbo.vw_TripSummary must already exist.
    Assumptions: a completed stop has a non-null, non-negative duration;
                 IsActive is the authoritative active-reference flag.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_TruckPerformance
AS
WITH TripMetrics AS (
    SELECT
        trip.TruckID,
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
    GROUP BY trip.TruckID
),
StopMetrics AS (
    SELECT
        stop.TruckID,
        COUNT_BIG(*) AS TotalStopCount,
        SUM(
            CONVERT(
                BIGINT,
                CASE WHEN stop.EndTime IS NOT NULL AND stop.DurationMinutes >= 0 THEN 1 ELSE 0 END
            )
        ) AS CompletedStopCount,
        SUM(CONVERT(BIGINT, COALESCE(stop.DurationMinutes, 0))) AS TotalDowntimeMinutes,
        CONVERT(
            DECIMAL(18,2),
            AVG(
                CASE
                    WHEN stop.DurationMinutes >= 0
                        THEN CONVERT(DECIMAL(18,2), stop.DurationMinutes)
                    ELSE NULL
                END
            )
        ) AS AverageStopDurationMinutes,
        MAX(CASE WHEN stop.DurationMinutes >= 0 THEN stop.DurationMinutes ELSE NULL END) AS MaximumStopDurationMinutes,
        SUM(CONVERT(BIGINT, CASE WHEN stop.TripID IS NOT NULL THEN 1 ELSE 0 END)) AS StopsWithTripCount,
        SUM(CONVERT(BIGINT, CASE WHEN stop.TripID IS NULL THEN 1 ELSE 0 END)) AS StopsWithoutTripCount
    FROM dbo.TruckStop AS stop
    GROUP BY stop.TruckID
),
QualityMetrics AS (
    SELECT
        trip.TruckID,
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
    GROUP BY trip.TruckID
)
SELECT
    truck.TruckID,
    truck.TruckCode,
    truck.Model AS TruckModel,
    truck.Status AS TruckStatus,
    truck.RatedPayloadTonnes,

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

    COALESCE(stopMetrics.TotalStopCount, CONVERT(BIGINT, 0)) AS TotalStopCount,
    COALESCE(stopMetrics.CompletedStopCount, CONVERT(BIGINT, 0)) AS CompletedStopCount,
    COALESCE(stopMetrics.TotalDowntimeMinutes, CONVERT(BIGINT, 0)) AS TotalDowntimeMinutes,
    stopMetrics.AverageStopDurationMinutes,
    stopMetrics.MaximumStopDurationMinutes,
    COALESCE(stopMetrics.StopsWithTripCount, CONVERT(BIGINT, 0)) AS StopsWithTripCount,
    COALESCE(stopMetrics.StopsWithoutTripCount, CONVERT(BIGINT, 0)) AS StopsWithoutTripCount,
    CONVERT(BIT, CASE WHEN stopMetrics.TotalStopCount > 0 THEN 1 ELSE 0 END) AS HasStops,

    COALESCE(qualityMetrics.QualityResultCount, CONVERT(BIGINT, 0)) AS QualityResultCount,
    qualityMetrics.AverageP2O5,
    qualityMetrics.MinimumP2O5,
    qualityMetrics.MaximumP2O5,
    CONVERT(BIT, CASE WHEN qualityMetrics.QualityResultCount > 0 THEN 1 ELSE 0 END) AS HasQualityResults
FROM dbo.Truck AS truck
LEFT JOIN TripMetrics AS tripMetrics
    ON tripMetrics.TruckID = truck.TruckID
LEFT JOIN StopMetrics AS stopMetrics
    ON stopMetrics.TruckID = truck.TruckID
LEFT JOIN QualityMetrics AS qualityMetrics
    ON qualityMetrics.TruckID = truck.TruckID;
GO
