/*
    Reporting 01 - Trip summary view
    Project: OCP_Mining_Prototype

    Grain: exactly one row per trip.
    Truck stops and P2O5 quality results are aggregated by TripID before
    joining to Trip, preventing one-to-many joins from multiplying rows.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_TripSummary
AS
WITH StopMetrics AS (
    SELECT
        stop.TripID,
        COUNT_BIG(*) AS StopCount,
        SUM(CONVERT(BIGINT, COALESCE(stop.DurationMinutes, 0))) AS TotalStopDurationMinutes,
        CONVERT(
            DECIMAL(18,2),
            AVG(CONVERT(DECIMAL(18,2), stop.DurationMinutes))
        ) AS AverageStopDurationMinutes,
        MAX(stop.DurationMinutes) AS MaximumStopDurationMinutes
    FROM dbo.TruckStop AS stop
    WHERE stop.TripID IS NOT NULL
    GROUP BY stop.TripID
),
RankedQualityResults AS (
    SELECT
        quality.TripID,
        quality.QualityValue,
        ROW_NUMBER() OVER (
            PARTITION BY quality.TripID
            ORDER BY quality.AnalysisDate DESC, quality.QualityResultID DESC
        ) AS LatestResultRank
    FROM dbo.QualityResult AS quality
    WHERE
        quality.TripID IS NOT NULL
        AND quality.QualityUnit = N'P2O5Percent'
),
QualityMetrics AS (
    SELECT
        quality.TripID,
        COUNT_BIG(*) AS QualityResultCount,
        CONVERT(
            DECIMAL(10,3),
            AVG(CONVERT(DECIMAL(18,3), quality.QualityValue))
        ) AS AverageP2O5,
        MIN(quality.QualityValue) AS MinimumP2O5,
        MAX(quality.QualityValue) AS MaximumP2O5,
        MAX(
            CASE
                WHEN quality.LatestResultRank = 1 THEN quality.QualityValue
                ELSE NULL
            END
        ) AS LatestP2O5
    FROM RankedQualityResults AS quality
    GROUP BY quality.TripID
)
SELECT
    trip.TripID,
    trip.TripDate,
    trip.Status,

    trip.TruckID,
    truck.TruckCode,
    truck.Model AS TruckModel,
    truck.RatedPayloadTonnes,

    trip.DriverID,
    driver.DriverCode,
    driver.FullName,

    trip.ShiftID,
    shiftRef.ShiftCode,
    shiftRef.ShiftName,

    trip.LoadingPointID,
    loading.LoadingPointCode,
    loading.LoadingPointName,

    trip.DestinationID,
    destination.DestinationCode,
    destination.DestinationName,

    trip.StartTime,
    trip.EndTime,
    CASE
        WHEN trip.EndTime IS NULL THEN NULL
        ELSE DATEDIFF(MINUTE, trip.StartTime, trip.EndTime)
    END AS TripDurationMinutes,

    trip.Tonnage,
    CONVERT(
        DECIMAL(10,2),
        CONVERT(DECIMAL(19,4), trip.Tonnage) * CONVERT(DECIMAL(5,2), 100.00)
            / NULLIF(CONVERT(DECIMAL(19,4), truck.RatedPayloadTonnes), 0)
    ) AS PayloadUtilizationPercent,

    COALESCE(stopMetrics.StopCount, CONVERT(BIGINT, 0)) AS StopCount,
    COALESCE(stopMetrics.TotalStopDurationMinutes, CONVERT(BIGINT, 0)) AS TotalStopDurationMinutes,
    stopMetrics.AverageStopDurationMinutes,
    stopMetrics.MaximumStopDurationMinutes,
    CONVERT(BIT, CASE WHEN stopMetrics.StopCount IS NULL THEN 0 ELSE 1 END) AS HasStops,

    COALESCE(qualityMetrics.QualityResultCount, CONVERT(BIGINT, 0)) AS QualityResultCount,
    qualityMetrics.AverageP2O5,
    qualityMetrics.MinimumP2O5,
    qualityMetrics.MaximumP2O5,
    qualityMetrics.LatestP2O5,
    CONVERT(BIT, CASE WHEN qualityMetrics.QualityResultCount IS NULL THEN 0 ELSE 1 END) AS HasQualityResult,

    CONVERT(BIT, CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END) AS IsCompleted,
    CONVERT(BIT, CASE WHEN trip.Status <> N'Completed' THEN 1 ELSE 0 END) AS IsIncomplete
FROM dbo.Trip AS trip
INNER JOIN dbo.Truck AS truck
    ON truck.TruckID = trip.TruckID
INNER JOIN dbo.Driver AS driver
    ON driver.DriverID = trip.DriverID
INNER JOIN dbo.WorkShift AS shiftRef
    ON shiftRef.ShiftID = trip.ShiftID
INNER JOIN dbo.LoadingPoint AS loading
    ON loading.LoadingPointID = trip.LoadingPointID
INNER JOIN dbo.Destination AS destination
    ON destination.DestinationID = trip.DestinationID
LEFT JOIN StopMetrics AS stopMetrics
    ON stopMetrics.TripID = trip.TripID
LEFT JOIN QualityMetrics AS qualityMetrics
    ON qualityMetrics.TripID = trip.TripID;
GO

/* Validation 1: the base-table and view row counts must be identical. */
SELECT COUNT(*)
FROM dbo.Trip;

/* Validation 2: must equal Validation 1. */
SELECT COUNT(*)
FROM dbo.vw_TripSummary;

/* Validation 3: expected result is zero rows. */
SELECT
    TripID,
    COUNT(*)
FROM dbo.vw_TripSummary
GROUP BY TripID
HAVING COUNT(*) > 1;

/* Validation 4: visual inspection sample. */
SELECT TOP (20) *
FROM dbo.vw_TripSummary;
GO
