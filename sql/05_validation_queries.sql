/*
    Script 05 - Validation queries

    Run after:
    1. sql/01_create_database.sql
    2. sql/02_seed_reference_data.sql
    3. sql/03_generate_sample_data.sql
    4. sql/04_create_reporting_views.sql
*/

USE OCP_Mining_Prototype;
GO

/* 1. Exact table counts. */
SELECT N'Truck' AS ObjectName, 12 AS ExpectedRows, COUNT(*) AS ActualRows FROM dbo.Truck
UNION ALL SELECT N'Driver', 20, COUNT(*) FROM dbo.Driver
UNION ALL SELECT N'WorkShift', 3, COUNT(*) FROM dbo.WorkShift
UNION ALL SELECT N'LoadingPoint', 4, COUNT(*) FROM dbo.LoadingPoint
UNION ALL SELECT N'Destination', 3, COUNT(*) FROM dbo.Destination
UNION ALL SELECT N'StopReason', 8, COUNT(*) FROM dbo.StopReason
UNION ALL SELECT N'Trip', 800, COUNT(*) FROM dbo.Trip
UNION ALL SELECT N'TruckStop', 150, COUNT(*) FROM dbo.TruckStop
UNION ALL SELECT N'QualityResult P2O5 analyses', 120, COUNT(*) FROM dbo.QualityResult;
GO

/* 2. Reference coverage. Expected result: all UnusedRows values are 0. */
SELECT N'Truck without trips' AS CheckName, COUNT(*) AS UnusedRows
FROM dbo.Truck AS ref
LEFT JOIN dbo.Trip AS trip ON trip.TruckID = ref.TruckID
WHERE trip.TripID IS NULL
UNION ALL
SELECT N'Driver without trips', COUNT(*)
FROM dbo.Driver AS ref
LEFT JOIN dbo.Trip AS trip ON trip.DriverID = ref.DriverID
WHERE trip.TripID IS NULL
UNION ALL
SELECT N'Shift without trips', COUNT(*)
FROM dbo.WorkShift AS ref
LEFT JOIN dbo.Trip AS trip ON trip.ShiftID = ref.ShiftID
WHERE trip.TripID IS NULL
UNION ALL
SELECT N'Loading point without trips', COUNT(*)
FROM dbo.LoadingPoint AS ref
LEFT JOIN dbo.Trip AS trip ON trip.LoadingPointID = ref.LoadingPointID
WHERE trip.TripID IS NULL
UNION ALL
SELECT N'Destination without trips', COUNT(*)
FROM dbo.Destination AS ref
LEFT JOIN dbo.Trip AS trip ON trip.DestinationID = ref.DestinationID
WHERE trip.TripID IS NULL
UNION ALL
SELECT N'Stop reason without stops', COUNT(*)
FROM dbo.StopReason AS ref
LEFT JOIN dbo.TruckStop AS stop ON stop.StopReasonID = ref.StopReasonID
WHERE stop.TruckStopID IS NULL;
GO

/* 3. Foreign-key consistency. Expected result: all MissingReferences values are 0. */
SELECT N'Trip.ShiftID' AS CheckName, COUNT(*) AS MissingReferences
FROM dbo.Trip AS trip
LEFT JOIN dbo.WorkShift AS ref ON ref.ShiftID = trip.ShiftID
WHERE ref.ShiftID IS NULL
UNION ALL
SELECT N'Trip.TruckID', COUNT(*)
FROM dbo.Trip AS trip
LEFT JOIN dbo.Truck AS ref ON ref.TruckID = trip.TruckID
WHERE ref.TruckID IS NULL
UNION ALL
SELECT N'Trip.DriverID', COUNT(*)
FROM dbo.Trip AS trip
LEFT JOIN dbo.Driver AS ref ON ref.DriverID = trip.DriverID
WHERE ref.DriverID IS NULL
UNION ALL
SELECT N'Trip.LoadingPointID', COUNT(*)
FROM dbo.Trip AS trip
LEFT JOIN dbo.LoadingPoint AS ref ON ref.LoadingPointID = trip.LoadingPointID
WHERE ref.LoadingPointID IS NULL
UNION ALL
SELECT N'Trip.DestinationID', COUNT(*)
FROM dbo.Trip AS trip
LEFT JOIN dbo.Destination AS ref ON ref.DestinationID = trip.DestinationID
WHERE ref.DestinationID IS NULL
UNION ALL
SELECT N'TruckStop.TruckID', COUNT(*)
FROM dbo.TruckStop AS stop
LEFT JOIN dbo.Truck AS ref ON ref.TruckID = stop.TruckID
WHERE ref.TruckID IS NULL
UNION ALL
SELECT N'TruckStop.DriverID', COUNT(*)
FROM dbo.TruckStop AS stop
LEFT JOIN dbo.Driver AS ref ON ref.DriverID = stop.DriverID
WHERE stop.DriverID IS NOT NULL AND ref.DriverID IS NULL
UNION ALL
SELECT N'TruckStop.ShiftID', COUNT(*)
FROM dbo.TruckStop AS stop
LEFT JOIN dbo.WorkShift AS ref ON ref.ShiftID = stop.ShiftID
WHERE stop.ShiftID IS NOT NULL AND ref.ShiftID IS NULL
UNION ALL
SELECT N'TruckStop.TripID', COUNT(*)
FROM dbo.TruckStop AS stop
LEFT JOIN dbo.Trip AS ref ON ref.TripID = stop.TripID
WHERE stop.TripID IS NOT NULL AND ref.TripID IS NULL
UNION ALL
SELECT N'TruckStop.StopReasonID', COUNT(*)
FROM dbo.TruckStop AS stop
LEFT JOIN dbo.StopReason AS ref ON ref.StopReasonID = stop.StopReasonID
WHERE ref.StopReasonID IS NULL
UNION ALL
SELECT N'QualityResult.TripID', COUNT(*)
FROM dbo.QualityResult AS quality
LEFT JOIN dbo.Trip AS ref ON ref.TripID = quality.TripID
WHERE quality.TripID IS NOT NULL AND ref.TripID IS NULL
UNION ALL
SELECT N'QualityResult.LoadingPointID', COUNT(*)
FROM dbo.QualityResult AS quality
LEFT JOIN dbo.LoadingPoint AS ref ON ref.LoadingPointID = quality.LoadingPointID
WHERE quality.LoadingPointID IS NOT NULL AND ref.LoadingPointID IS NULL;
GO

/* 4. Invalid times. Expected result: 0 rows. */
SELECT N'Trip' AS ObjectName, TripID AS RecordID, StartTime, EndTime
FROM dbo.Trip
WHERE EndTime IS NOT NULL AND EndTime <= StartTime
UNION ALL
SELECT N'TruckStop', TruckStopID, StartTime, EndTime
FROM dbo.TruckStop
WHERE EndTime IS NOT NULL AND EndTime <= StartTime;
GO

/* 5. Required-field checks. Expected ProblemRows values are 0. */
SELECT N'Trip required fields' AS CheckName, COUNT(*) AS ProblemRows
FROM dbo.Trip
WHERE
    TripDate IS NULL
    OR ShiftID IS NULL
    OR TruckID IS NULL
    OR DriverID IS NULL
    OR LoadingPointID IS NULL
    OR DestinationID IS NULL
    OR StartTime IS NULL
    OR Status IS NULL
UNION ALL
SELECT N'Completed trips missing positive EndTime or Tonnage', COUNT(*)
FROM dbo.Trip
WHERE Status = N'Completed' AND (EndTime IS NULL OR Tonnage IS NULL OR Tonnage <= 0)
UNION ALL
SELECT N'TruckStop required fields', COUNT(*)
FROM dbo.TruckStop
WHERE TruckID IS NULL OR StopReasonID IS NULL OR StartTime IS NULL OR EndTime IS NULL OR ValidationStatus IS NULL
UNION ALL
SELECT N'QualityResult required fields', COUNT(*)
FROM dbo.QualityResult
WHERE
    AnalysisDate IS NULL
    OR QualityValue IS NULL
    OR QualityUnit IS NULL
    OR Status IS NULL
    OR (TripID IS NULL AND LoadingPointID IS NULL);
GO

/* 6. Duplicate business records. Expected DuplicateGroups values are 0. */
SELECT N'Trip same TruckID and StartTime' AS CheckName, COUNT(*) AS DuplicateGroups
FROM (
    SELECT TruckID, StartTime
    FROM dbo.Trip
    GROUP BY TruckID, StartTime
    HAVING COUNT(*) > 1
) AS duplicates
UNION ALL
SELECT N'TruckStop same TruckID, StartTime, StopReasonID', COUNT(*)
FROM (
    SELECT TruckID, StartTime, StopReasonID
    FROM dbo.TruckStop
    GROUP BY TruckID, StartTime, StopReasonID
    HAVING COUNT(*) > 1
) AS duplicates
UNION ALL
SELECT N'QualityResult same AnalysisDate, TripID, LoadingPointID', COUNT(*)
FROM (
    SELECT AnalysisDate, TripID, LoadingPointID
    FROM dbo.QualityResult
    GROUP BY AnalysisDate, TripID, LoadingPointID
    HAVING COUNT(*) > 1
) AS duplicates;
GO

/* 7. Trip realism: daily counts should vary between about 20 and 35. */
SELECT
    TripDate,
    COUNT(*) AS TotalTrips,
    SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
    SUM(CASE WHEN Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
    CONVERT(DECIMAL(18,2), SUM(COALESCE(Tonnage, 0))) AS TotalTonnage
FROM dbo.Trip
GROUP BY TripDate
ORDER BY TripDate;
GO

/* 8. Trip realism: status, long-trip rate, duration, and tonnage summaries. */
SELECT
    COUNT(*) AS TotalTrips,
    SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrips,
    SUM(CASE WHEN Status <> N'Completed' THEN 1 ELSE 0 END) AS IncompleteTrips,
    CONVERT(DECIMAL(10,4), CONVERT(DECIMAL(18,4), SUM(CASE WHEN Status <> N'Completed' THEN 1 ELSE 0 END)) / COUNT(*)) AS IncompleteTripRate,
    SUM(CASE WHEN Status = N'Completed' AND DATEDIFF(MINUTE, StartTime, EndTime) BETWEEN 31 AND 50 THEN 1 ELSE 0 END) AS LongTripCount,
    CONVERT(DECIMAL(10,4), CONVERT(DECIMAL(18,4), SUM(CASE WHEN Status = N'Completed' AND DATEDIFF(MINUTE, StartTime, EndTime) BETWEEN 31 AND 50 THEN 1 ELSE 0 END)) / SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END)) AS LongTripRate,
    MIN(CASE WHEN Status = N'Completed' THEN DATEDIFF(MINUTE, StartTime, EndTime) END) AS MinimumTripDurationMinutes,
    MAX(CASE WHEN Status = N'Completed' THEN DATEDIFF(MINUTE, StartTime, EndTime) END) AS MaximumTripDurationMinutes,
    CONVERT(DECIMAL(18,2), AVG(CASE WHEN Status = N'Completed' THEN CONVERT(DECIMAL(18,2), DATEDIFF(MINUTE, StartTime, EndTime)) END)) AS AverageTripDurationMinutes,
    MIN(CASE WHEN Status = N'Completed' THEN Tonnage END) AS MinimumTonnage,
    MAX(CASE WHEN Status = N'Completed' THEN Tonnage END) AS MaximumTonnage,
    CONVERT(DECIMAL(18,2), AVG(CASE WHEN Status = N'Completed' THEN Tonnage END)) AS AverageTonnage
FROM dbo.Trip;
GO

/* 9. Payload compatibility. Expected result: 0 rows. */
SELECT
    trip.TripID,
    truck.TruckCode,
    truck.RatedPayloadTonnes,
    trip.Tonnage
FROM dbo.Trip AS trip
INNER JOIN dbo.Truck AS truck
    ON truck.TruckID = trip.TruckID
WHERE
    trip.Status = N'Completed'
    AND (
        trip.Tonnage < 50.00
        OR trip.Tonnage > 90.00
        OR trip.Tonnage > truck.RatedPayloadTonnes * 1.05
    );
GO

/* 10. Distribution by shift, destination, truck, and driver. */
SELECT ShiftCode, ShiftName, COUNT(*) AS TotalTrips
FROM dbo.vw_TripDetails
GROUP BY ShiftCode, ShiftName
ORDER BY ShiftCode;

SELECT DestinationCode, DestinationName, COUNT(*) AS TotalTrips
FROM dbo.vw_TripDetails
GROUP BY DestinationCode, DestinationName
ORDER BY TotalTrips DESC;

SELECT TruckCode, TotalTrips, CompletedTrips, TotalTonnage, AverageTripDurationMinutes
FROM dbo.vw_TruckPerformance
ORDER BY TotalTrips DESC, TruckCode;

SELECT DriverCode, FullName, TotalTrips, CompletedTrips, TotalTonnage, AverageTripDurationMinutes, AverageTripsPerShift
FROM dbo.vw_DriverPerformance
ORDER BY TotalTrips DESC, DriverCode;
GO

/* 11. Stop realism: duration by stop reason. */
SELECT
    ReasonCode,
    ReasonName,
    ReasonCategory,
    IsPlanned,
    TotalStops,
    TotalDowntimeMinutes,
    AverageStopDurationMinutes
FROM dbo.vw_StopsByReason
ORDER BY TotalStops DESC, ReasonCode;
GO

/* 12. Stop realism: mechanical stops should average longer than operational waits. */
SELECT
    ReasonCategory,
    COUNT(*) AS NumberOfStops,
    SUM(DurationMinutes) AS TotalDowntimeMinutes,
    CONVERT(DECIMAL(18,2), AVG(CONVERT(DECIMAL(18,2), DurationMinutes))) AS AverageStopDurationMinutes
FROM dbo.vw_TruckStopDetails
GROUP BY ReasonCategory
ORDER BY AverageStopDurationMinutes DESC;
GO

/* 13. Planned-status consistency. Expected result: 0 rows. */
SELECT
    ReasonCode,
    ReasonName,
    ReasonCategory,
    IsPlanned
FROM dbo.StopReason
WHERE
    (ReasonCode = N'PREVENTIVE_MAINTENANCE' AND IsPlanned <> 1)
    OR (ReasonCode <> N'PREVENTIVE_MAINTENANCE' AND IsPlanned <> 0);
GO

/* 14. Quality realism: P2O5 summary and impossible values. */
SELECT
    QualityUnit,
    COUNT(*) AS NumberOfAnalyses,
    CONVERT(DECIMAL(10,3), AVG(CONVERT(DECIMAL(18,3), QualityValue))) AS AverageP2O5,
    MIN(QualityValue) AS MinimumP2O5,
    MAX(QualityValue) AS MaximumP2O5
FROM dbo.QualityResult
GROUP BY QualityUnit;

SELECT
    QualityResultID,
    QualityValue,
    QualityUnit
FROM dbo.QualityResult
WHERE
    QualityValue < 0
    OR (QualityUnit = N'P2O5Percent' AND (QualityValue < 28.500 OR QualityValue > 31.500));
GO

/* 15. Quality by period and loading point. */
SELECT
    AnalysisDate,
    LoadingPointCode,
    ZoneName,
    QualityUnit,
    NumberOfAnalyses,
    AverageQuality,
    MinimumQuality,
    MaximumQuality
FROM dbo.vw_QualityByPeriod
ORDER BY AnalysisDate, LoadingPointCode;
GO

/* 16. Reporting consistency: base tables versus views. Expected Difference values are 0. */
SELECT N'Trip base vs vw_TripDetails' AS CheckName, baseCounts.TotalRows - viewCounts.TotalRows AS Difference
FROM (SELECT COUNT(*) AS TotalRows FROM dbo.Trip) AS baseCounts
CROSS JOIN (SELECT COUNT(*) AS TotalRows FROM dbo.vw_TripDetails) AS viewCounts
UNION ALL
SELECT N'TruckStop base vs vw_TruckStopDetails', baseCounts.TotalRows - viewCounts.TotalRows
FROM (SELECT COUNT(*) AS TotalRows FROM dbo.TruckStop) AS baseCounts
CROSS JOIN (SELECT COUNT(*) AS TotalRows FROM dbo.vw_TruckStopDetails) AS viewCounts
UNION ALL
SELECT N'Trip base vs truck performance sum', baseCounts.TotalRows - viewCounts.TotalRows
FROM (SELECT COUNT(*) AS TotalRows FROM dbo.Trip) AS baseCounts
CROSS JOIN (SELECT SUM(TotalTrips) AS TotalRows FROM dbo.vw_TruckPerformance) AS viewCounts
UNION ALL
SELECT N'TruckStop base vs truck performance stop sum', baseCounts.TotalRows - viewCounts.TotalRows
FROM (SELECT COUNT(*) AS TotalRows FROM dbo.TruckStop) AS baseCounts
CROSS JOIN (SELECT SUM(TotalStops) AS TotalRows FROM dbo.vw_TruckPerformance) AS viewCounts
UNION ALL
SELECT N'Trip base vs driver performance sum', baseCounts.TotalRows - viewCounts.TotalRows
FROM (SELECT COUNT(*) AS TotalRows FROM dbo.Trip) AS baseCounts
CROSS JOIN (SELECT SUM(TotalTrips) AS TotalRows FROM dbo.vw_DriverPerformance) AS viewCounts;
GO

/* 17. Global dashboard summary. */
SELECT *
FROM dbo.vw_GlobalProductionSummary;
GO

