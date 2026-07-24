/*
    View: dbo.vw_StopAnalysis
    Objective: preserve stop-level detail for Power BI filtering, Pareto,
               trend, duration, and linked/unlinked trip analysis.
    Grain: one row per dbo.TruckStop row.
    Sources: dbo.TruckStop, dbo.Truck, dbo.StopReason, and optional trip context.
    Rules: all stops are retained; duration is NULL for open or invalid intervals;
           driver and shift context comes only from the explicitly linked trip.
    Dependencies: base tables only.
    Assumptions: StopReason.IsPlanned is the authoritative planning flag.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_StopAnalysis
AS
WITH StopBase AS (
    SELECT
        stop.TruckStopID,
        stop.TruckID,
        stop.TripID,
        stop.StopReasonID,
        stop.StartTime,
        stop.EndTime,
        CASE
            WHEN stop.EndTime IS NULL OR stop.EndTime < stop.StartTime THEN NULL
            ELSE DATEDIFF(MINUTE, stop.StartTime, stop.EndTime)
        END AS StopDurationMinutes,
        stop.ValidationStatus
    FROM dbo.TruckStop AS stop
)
SELECT
    stop.TruckStopID,
    stop.TruckID,
    truck.TruckCode,
    stop.TripID,
    stop.StopReasonID,
    reason.ReasonCode AS StopReasonCode,
    reason.ReasonName AS StopReasonName,

    reason.Category AS StopCategory,
    reason.IsPlanned,
    CONVERT(
        NVARCHAR(10),
        CASE WHEN reason.IsPlanned = 1 THEN N'Planned' ELSE N'Unplanned' END
    ) AS PlannedUnplannedLabel,
    CONVERT(BIT, CASE WHEN stop.TripID IS NOT NULL THEN 1 ELSE 0 END) AS HasLinkedTrip,
    stop.ValidationStatus AS StopStatus,

    stop.StartTime AS StopStartTime,
    stop.EndTime AS StopEndTime,
    CONVERT(DATE, stop.StartTime) AS StopDate,
    DATEPART(YEAR, stop.StartTime) AS StopYear,
    DATEPART(MONTH, stop.StartTime) AS StopMonthNumber,
    stop.StopDurationMinutes,
    CONVERT(
        BIT,
        CASE WHEN stop.EndTime IS NOT NULL AND stop.StopDurationMinutes IS NOT NULL THEN 1 ELSE 0 END
    ) AS IsCompletedStop,
    CONVERT(BIT, CASE WHEN stop.EndTime IS NULL THEN 1 ELSE 0 END) AS IsOpenStop,

    trip.TripDate,
    trip.DriverID,
    driver.DriverCode,
    driver.FullName AS DriverFullName,
    trip.ShiftID,
    shiftRef.ShiftCode,
    shiftRef.ShiftName,
    trip.LoadingPointID,
    loading.LoadingPointCode,
    loading.LoadingPointName,
    trip.DestinationID,
    destination.DestinationCode,
    destination.DestinationName,
    trip.Status AS TripStatus
FROM StopBase AS stop
INNER JOIN dbo.Truck AS truck
    ON truck.TruckID = stop.TruckID
INNER JOIN dbo.StopReason AS reason
    ON reason.StopReasonID = stop.StopReasonID
LEFT JOIN dbo.Trip AS trip
    ON trip.TripID = stop.TripID
LEFT JOIN dbo.Driver AS driver
    ON driver.DriverID = trip.DriverID
LEFT JOIN dbo.WorkShift AS shiftRef
    ON shiftRef.ShiftID = trip.ShiftID
LEFT JOIN dbo.LoadingPoint AS loading
    ON loading.LoadingPointID = trip.LoadingPointID
LEFT JOIN dbo.Destination AS destination
    ON destination.DestinationID = trip.DestinationID;
GO
