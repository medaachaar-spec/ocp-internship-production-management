/*
    Script 04 - Create reporting views

    Run after sql/01_create_database.sql.
    The views are compatible with the sample data from sql/03_generate_sample_data.sql
    and can later point to real imported data with the same schema.
*/

USE OCP_Mining_Prototype;
GO

CREATE OR ALTER VIEW dbo.vw_TripDetails
AS
SELECT
    trip.TripID,
    trip.TripDate,
    trip.ShiftID,
    shiftRef.ShiftCode,
    shiftRef.ShiftName,
    trip.TruckID,
    truck.TruckCode,
    truck.RegistrationNumber,
    trip.DriverID,
    driver.DriverCode,
    driver.FullName AS DriverName,
    trip.LoadingPointID,
    loading.LoadingPointCode,
    loading.LoadingPointName,
    loading.ZoneName,
    loading.LoadingPointDescription,
    trip.DestinationID,
    destination.DestinationCode,
    destination.DestinationName,
    destination.DestinationType,
    trip.StartTime,
    trip.EndTime,
    CASE
        WHEN trip.EndTime IS NULL THEN NULL
        ELSE DATEDIFF(MINUTE, trip.StartTime, trip.EndTime)
    END AS DurationMinutes,
    CASE
        WHEN trip.Status = N'Completed'
            AND trip.EndTime IS NOT NULL
            AND DATEDIFF(MINUTE, trip.StartTime, trip.EndTime) > 30 THEN 1
        ELSE 0
    END AS IsLongTrip,
    trip.Tonnage,
    trip.Status,
    trip.Comments
FROM dbo.Trip AS trip
INNER JOIN dbo.WorkShift AS shiftRef
    ON shiftRef.ShiftID = trip.ShiftID
INNER JOIN dbo.Truck AS truck
    ON truck.TruckID = trip.TruckID
INNER JOIN dbo.Driver AS driver
    ON driver.DriverID = trip.DriverID
INNER JOIN dbo.LoadingPoint AS loading
    ON loading.LoadingPointID = trip.LoadingPointID
INNER JOIN dbo.Destination AS destination
    ON destination.DestinationID = trip.DestinationID;
GO

CREATE OR ALTER VIEW dbo.vw_TripsDailySummary
AS
SELECT
    trip.TripDate,
    COUNT(*) AS TotalTrips,
    SUM(CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
    SUM(CASE WHEN trip.Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
    CONVERT(DECIMAL(18,2), SUM(CASE WHEN trip.Status = N'Completed' THEN COALESCE(trip.Tonnage, 0) ELSE 0 END)) AS TotalTonnage,
    CONVERT(DECIMAL(18,2), AVG(
        CASE
            WHEN trip.Status = N'Completed' AND trip.EndTime IS NOT NULL THEN
                CONVERT(DECIMAL(18,2), DATEDIFF(MINUTE, trip.StartTime, trip.EndTime))
            ELSE NULL
        END
    )) AS AverageTripDurationMinutes,
    SUM(
        CASE
            WHEN trip.Status = N'Completed'
                AND trip.EndTime IS NOT NULL
                AND DATEDIFF(MINUTE, trip.StartTime, trip.EndTime) > 30 THEN 1
            ELSE 0
        END
    ) AS LongTripCount,
    CASE
        WHEN SUM(CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END) = 0 THEN NULL
        ELSE CONVERT(
            DECIMAL(10,4),
            CONVERT(
                DECIMAL(18,4),
                SUM(
                    CASE
                        WHEN trip.Status = N'Completed'
                            AND trip.EndTime IS NOT NULL
                            AND DATEDIFF(MINUTE, trip.StartTime, trip.EndTime) > 30 THEN 1
                        ELSE 0
                    END
                )
            ) / SUM(CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END)
        )
    END AS LongTripRate
FROM dbo.Trip AS trip
GROUP BY trip.TripDate;
GO

CREATE OR ALTER VIEW dbo.vw_TruckStopDetails
AS
SELECT
    stop.TruckStopID,
    CAST(stop.StartTime AS DATE) AS StopDate,
    stop.TruckID,
    truck.TruckCode,
    stop.DriverID,
    driver.DriverCode,
    driver.FullName AS DriverName,
    stop.ShiftID,
    shiftRef.ShiftCode,
    shiftRef.ShiftName,
    stop.TripID,
    stop.StopReasonID,
    reason.ReasonCode,
    reason.ReasonName,
    reason.Category AS ReasonCategory,
    reason.IsPlanned,
    stop.StartTime,
    stop.EndTime,
    stop.DurationMinutes,
    stop.ValidationStatus,
    stop.Comments
FROM dbo.TruckStop AS stop
INNER JOIN dbo.Truck AS truck
    ON truck.TruckID = stop.TruckID
LEFT JOIN dbo.Driver AS driver
    ON driver.DriverID = stop.DriverID
LEFT JOIN dbo.WorkShift AS shiftRef
    ON shiftRef.ShiftID = stop.ShiftID
INNER JOIN dbo.StopReason AS reason
    ON reason.StopReasonID = stop.StopReasonID;
GO

CREATE OR ALTER VIEW dbo.vw_StopsByReason
AS
SELECT
    reason.StopReasonID,
    reason.ReasonCode,
    reason.ReasonName,
    reason.Category AS ReasonCategory,
    reason.IsPlanned,
    COUNT(stop.TruckStopID) AS TotalStops,
    SUM(COALESCE(stop.DurationMinutes, 0)) AS TotalDowntimeMinutes,
    CONVERT(DECIMAL(18,2), AVG(CONVERT(DECIMAL(18,2), stop.DurationMinutes))) AS AverageStopDurationMinutes
FROM dbo.StopReason AS reason
LEFT JOIN dbo.TruckStop AS stop
    ON stop.StopReasonID = reason.StopReasonID
GROUP BY
    reason.StopReasonID,
    reason.ReasonCode,
    reason.ReasonName,
    reason.Category,
    reason.IsPlanned;
GO

CREATE OR ALTER VIEW dbo.vw_TruckPerformance
AS
WITH TripMetrics AS (
    SELECT
        trip.TruckID,
        COUNT(*) AS TotalTrips,
        SUM(CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
        SUM(CASE WHEN trip.Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
        SUM(CASE WHEN trip.Status = N'Completed' THEN COALESCE(trip.Tonnage, 0) ELSE 0 END) AS TotalTonnage,
        AVG(
            CASE
                WHEN trip.Status = N'Completed' AND trip.EndTime IS NOT NULL THEN
                    CONVERT(DECIMAL(18,2), DATEDIFF(MINUTE, trip.StartTime, trip.EndTime))
                ELSE NULL
            END
        ) AS AverageTripDurationMinutes,
        SUM(
            CASE
                WHEN trip.Status = N'Completed'
                    AND trip.EndTime IS NOT NULL
                    AND DATEDIFF(MINUTE, trip.StartTime, trip.EndTime) > 30 THEN 1
                ELSE 0
            END
        ) AS LongTripCount
    FROM dbo.Trip AS trip
    GROUP BY trip.TruckID
),
StopMetrics AS (
    SELECT
        stop.TruckID,
        COUNT(*) AS TotalStops,
        SUM(COALESCE(stop.DurationMinutes, 0)) AS TotalDowntimeMinutes,
        AVG(
            CASE
                WHEN stop.DurationMinutes IS NULL THEN NULL
                ELSE CONVERT(DECIMAL(18,2), stop.DurationMinutes)
            END
        ) AS AverageStopDurationMinutes
    FROM dbo.TruckStop AS stop
    GROUP BY stop.TruckID
)
SELECT
    truck.TruckID,
    truck.TruckCode,
    truck.RegistrationNumber,
    truck.Model,
    truck.RatedPayloadTonnes,
    COALESCE(tripMetrics.TotalTrips, 0) AS TotalTrips,
    COALESCE(tripMetrics.CompletedTrips, 0) AS CompletedTrips,
    COALESCE(tripMetrics.IncompleteTrips, 0) AS IncompleteTrips,
    CONVERT(DECIMAL(18,2), COALESCE(tripMetrics.TotalTonnage, 0)) AS TotalTonnage,
    CONVERT(DECIMAL(18,2), tripMetrics.AverageTripDurationMinutes) AS AverageTripDurationMinutes,
    COALESCE(tripMetrics.LongTripCount, 0) AS LongTripCount,
    CASE
        WHEN COALESCE(tripMetrics.CompletedTrips, 0) = 0 THEN NULL
        ELSE CONVERT(DECIMAL(10,4), CONVERT(DECIMAL(18,4), tripMetrics.LongTripCount) / tripMetrics.CompletedTrips)
    END AS LongTripRate,
    COALESCE(stopMetrics.TotalStops, 0) AS TotalStops,
    COALESCE(stopMetrics.TotalDowntimeMinutes, 0) AS TotalDowntimeMinutes,
    CONVERT(DECIMAL(18,2), stopMetrics.AverageStopDurationMinutes) AS AverageStopDurationMinutes
FROM dbo.Truck AS truck
LEFT JOIN TripMetrics AS tripMetrics
    ON tripMetrics.TruckID = truck.TruckID
LEFT JOIN StopMetrics AS stopMetrics
    ON stopMetrics.TruckID = truck.TruckID;
GO

CREATE OR ALTER VIEW dbo.vw_DriverPerformance
AS
WITH DriverTripMetrics AS (
    SELECT
        trip.DriverID,
        COUNT(*) AS TotalTrips,
        SUM(CASE WHEN trip.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
        SUM(CASE WHEN trip.Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
        SUM(CASE WHEN trip.Status = N'Completed' THEN COALESCE(trip.Tonnage, 0) ELSE 0 END) AS TotalTonnage,
        AVG(
            CASE
                WHEN trip.Status = N'Completed' AND trip.EndTime IS NOT NULL THEN
                    CONVERT(DECIMAL(18,2), DATEDIFF(MINUTE, trip.StartTime, trip.EndTime))
                ELSE NULL
            END
        ) AS AverageTripDurationMinutes
    FROM dbo.Trip AS trip
    GROUP BY trip.DriverID
),
DriverShiftMetrics AS (
    SELECT
        trip.DriverID,
        COUNT(DISTINCT CONCAT(CONVERT(CHAR(10), trip.TripDate, 23), N'|', CONVERT(NVARCHAR(20), trip.ShiftID))) AS WorkedShifts
    FROM dbo.Trip AS trip
    GROUP BY trip.DriverID
)
SELECT
    driver.DriverID,
    driver.DriverCode,
    driver.FullName,
    COALESCE(tripMetrics.TotalTrips, 0) AS TotalTrips,
    COALESCE(tripMetrics.CompletedTrips, 0) AS CompletedTrips,
    COALESCE(tripMetrics.IncompleteTrips, 0) AS IncompleteTrips,
    CONVERT(DECIMAL(18,2), COALESCE(tripMetrics.TotalTonnage, 0)) AS TotalTonnage,
    CONVERT(DECIMAL(18,2), tripMetrics.AverageTripDurationMinutes) AS AverageTripDurationMinutes,
    COALESCE(shiftMetrics.WorkedShifts, 0) AS WorkedShifts,
    CASE
        WHEN COALESCE(shiftMetrics.WorkedShifts, 0) = 0 THEN NULL
        ELSE CONVERT(DECIMAL(10,2), CONVERT(DECIMAL(18,2), tripMetrics.TotalTrips) / shiftMetrics.WorkedShifts)
    END AS AverageTripsPerShift
FROM dbo.Driver AS driver
LEFT JOIN DriverTripMetrics AS tripMetrics
    ON tripMetrics.DriverID = driver.DriverID
LEFT JOIN DriverShiftMetrics AS shiftMetrics
    ON shiftMetrics.DriverID = driver.DriverID;
GO

CREATE OR ALTER VIEW dbo.vw_QualityByPeriod
AS
WITH QualityBase AS (
    SELECT
        quality.QualityResultID,
        quality.AnalysisDate,
        quality.TripID,
        COALESCE(quality.LoadingPointID, trip.LoadingPointID) AS EffectiveLoadingPointID,
        quality.QualityValue,
        quality.QualityUnit,
        quality.TargetValue,
        quality.Status
    FROM dbo.QualityResult AS quality
    LEFT JOIN dbo.Trip AS trip
        ON trip.TripID = quality.TripID
)
SELECT
    CAST(base.AnalysisDate AS DATE) AS AnalysisDate,
    loading.LoadingPointID,
    loading.LoadingPointCode,
    loading.LoadingPointName,
    loading.ZoneName,
    base.QualityUnit,
    COUNT(*) AS NumberOfAnalyses,
    CONVERT(DECIMAL(10,3), AVG(CONVERT(DECIMAL(18,3), base.QualityValue))) AS AverageQuality,
    MIN(base.QualityValue) AS MinimumQuality,
    MAX(base.QualityValue) AS MaximumQuality,
    SUM(CASE WHEN base.Status = N'InTarget' THEN 1 ELSE 0 END) AS InTargetCount,
    SUM(CASE WHEN base.Status <> N'InTarget' THEN 1 ELSE 0 END) AS OutOfTargetOrReviewCount
FROM QualityBase AS base
LEFT JOIN dbo.LoadingPoint AS loading
    ON loading.LoadingPointID = base.EffectiveLoadingPointID
GROUP BY
    CAST(base.AnalysisDate AS DATE),
    loading.LoadingPointID,
    loading.LoadingPointCode,
    loading.LoadingPointName,
    loading.ZoneName,
    base.QualityUnit;
GO

CREATE OR ALTER VIEW dbo.vw_GlobalProductionSummary
AS
WITH TripTotals AS (
    SELECT
        COUNT(*) AS TotalTrips,
        SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
        SUM(CASE WHEN Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
        CONVERT(DECIMAL(18,2), SUM(CASE WHEN Status = N'Completed' THEN COALESCE(Tonnage, 0) ELSE 0 END)) AS TotalTonnage,
        CONVERT(DECIMAL(18,2), AVG(
            CASE
                WHEN Status = N'Completed' AND EndTime IS NOT NULL THEN
                    CONVERT(DECIMAL(18,2), DATEDIFF(MINUTE, StartTime, EndTime))
                ELSE NULL
            END
        )) AS AverageTripDurationMinutes,
        SUM(
            CASE
                WHEN Status = N'Completed'
                    AND EndTime IS NOT NULL
                    AND DATEDIFF(MINUTE, StartTime, EndTime) > 30 THEN 1
                ELSE 0
            END
        ) AS LongTripCount
    FROM dbo.Trip
),
StopTotals AS (
    SELECT
        COUNT(*) AS TotalStops,
        SUM(COALESCE(DurationMinutes, 0)) AS TotalDowntimeMinutes,
        CONVERT(DECIMAL(18,2), AVG(CONVERT(DECIMAL(18,2), DurationMinutes))) AS AverageStopDurationMinutes
    FROM dbo.TruckStop
),
QualityTotals AS (
    SELECT
        COUNT(*) AS NumberOfAnalyses,
        CONVERT(DECIMAL(10,3), AVG(CONVERT(DECIMAL(18,3), QualityValue))) AS AverageP2O5,
        MIN(QualityValue) AS MinimumP2O5,
        MAX(QualityValue) AS MaximumP2O5
    FROM dbo.QualityResult
    WHERE QualityUnit = N'P2O5Percent'
)
SELECT
    tripTotals.TotalTrips,
    tripTotals.CompletedTrips,
    tripTotals.IncompleteTrips,
    tripTotals.TotalTonnage,
    tripTotals.AverageTripDurationMinutes,
    tripTotals.LongTripCount,
    CASE
        WHEN tripTotals.CompletedTrips = 0 THEN NULL
        ELSE CONVERT(DECIMAL(10,4), CONVERT(DECIMAL(18,4), tripTotals.LongTripCount) / tripTotals.CompletedTrips)
    END AS LongTripRate,
    stopTotals.TotalStops,
    stopTotals.TotalDowntimeMinutes,
    stopTotals.AverageStopDurationMinutes,
    qualityTotals.NumberOfAnalyses,
    qualityTotals.AverageP2O5,
    qualityTotals.MinimumP2O5,
    qualityTotals.MaximumP2O5
FROM TripTotals AS tripTotals
CROSS JOIN StopTotals AS stopTotals
CROSS JOIN QualityTotals AS qualityTotals;
GO
