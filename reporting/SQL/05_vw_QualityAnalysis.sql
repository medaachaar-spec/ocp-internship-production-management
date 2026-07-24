/*
    View: dbo.vw_QualityAnalysis
    Objective: preserve quality-result detail for Power BI trends,
               distributions, and linked/unlinked trip analysis.
    Grain: one row per dbo.QualityResult row.
    Sources: dbo.QualityResult and optional trip/reference context.
    Rules: all quality units are retained; P2O5Percent is exposed only for
           the P2O5Percent unit; latest-per-trip ordering is deterministic.
    Dependencies: base tables only.
    Assumptions: direct QualityResult.LoadingPointID takes precedence, with
                 Trip.LoadingPointID used only when the direct key is NULL.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_QualityAnalysis
AS
WITH RankedQuality AS (
    SELECT
        quality.QualityResultID,
        quality.AnalysisDate,
        quality.TripID,
        quality.LoadingPointID,
        quality.QualityValue,
        quality.QualityUnit,
        quality.TargetValue,
        quality.Status,
        ROW_NUMBER() OVER (
            PARTITION BY quality.TripID
            ORDER BY quality.AnalysisDate, quality.QualityResultID
        ) AS AnalysisSequence,
        ROW_NUMBER() OVER (
            PARTITION BY quality.TripID
            ORDER BY quality.AnalysisDate DESC, quality.QualityResultID DESC
        ) AS LatestSequence,
        COUNT_BIG(*) OVER (PARTITION BY quality.TripID) AS ResultsForPartition
    FROM dbo.QualityResult AS quality
)
SELECT
    quality.QualityResultID,
    quality.TripID,
    COALESCE(quality.LoadingPointID, trip.LoadingPointID) AS LoadingPointID,
    quality.AnalysisDate,
    CONVERT(DATE, quality.AnalysisDate) AS QualityDate,
    quality.QualityUnit,
    CASE
        WHEN quality.QualityUnit = N'P2O5Percent' THEN quality.QualityValue
        ELSE NULL
    END AS P2O5Percent,
    quality.TargetValue,
    quality.Status AS QualityStatus,

    CONVERT(BIT, CASE WHEN quality.TripID IS NOT NULL THEN 1 ELSE 0 END) AS HasLinkedTrip,
    trip.TripDate,
    trip.Status AS TripStatus,
    trip.TruckID,
    truck.TruckCode,
    trip.DriverID,
    driver.DriverCode,
    driver.FullName AS DriverFullName,
    trip.ShiftID,
    shiftRef.ShiftCode,
    shiftRef.ShiftName,
    loading.LoadingPointCode,
    loading.LoadingPointName,
    trip.DestinationID,
    destination.DestinationCode,
    destination.DestinationName,

    CONVERT(BIT, CASE WHEN quality.QualityUnit = N'P2O5Percent' THEN 1 ELSE 0 END) AS IsP2O5Measurement,
    CONVERT(
        BIT,
        CASE
            WHEN quality.QualityUnit = N'P2O5Percent' AND quality.QualityValue >= 0 THEN 1
            ELSE 0
        END
    ) AS HasValidP2O5,
    CONVERT(
        BIT,
        CASE WHEN quality.TripID IS NOT NULL AND quality.LatestSequence = 1 THEN 1 ELSE 0 END
    ) AS LatestResultForTrip,
    CASE
        WHEN quality.TripID IS NULL THEN CONVERT(BIGINT, NULL)
        ELSE quality.AnalysisSequence
    END AS AnalysisSequenceForTrip,
    CASE
        WHEN quality.TripID IS NULL THEN CONVERT(BIGINT, NULL)
        ELSE quality.ResultsForPartition
    END AS ResultCountForTrip
FROM RankedQuality AS quality
LEFT JOIN dbo.Trip AS trip
    ON trip.TripID = quality.TripID
LEFT JOIN dbo.Truck AS truck
    ON truck.TruckID = trip.TruckID
LEFT JOIN dbo.Driver AS driver
    ON driver.DriverID = trip.DriverID
LEFT JOIN dbo.WorkShift AS shiftRef
    ON shiftRef.ShiftID = trip.ShiftID
LEFT JOIN dbo.LoadingPoint AS loading
    ON loading.LoadingPointID = COALESCE(quality.LoadingPointID, trip.LoadingPointID)
LEFT JOIN dbo.Destination AS destination
    ON destination.DestinationID = trip.DestinationID;
GO
