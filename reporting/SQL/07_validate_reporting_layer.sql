/*
    Script: Reporting layer validation
    Objective: validate existence, grain, reconciliation, and KPI consistency
               for all Power BI reporting views without changing any data.
    Scope: dbo.vw_TripSummary, dbo.vw_TruckPerformance,
           dbo.vw_DriverPerformance, dbo.vw_StopAnalysis,
           dbo.vw_QualityAnalysis, dbo.vw_ExecutiveDashboard.
    Dependencies: reporting scripts 01 through 06 must be executed first.
    Assumptions: IsIncomplete means every Trip.Status other than Completed;
                 vw_TripSummary quality coverage is P2O5-specific.
    Creation date: 2026-07-18.
*/

USE OCP_Mining_Prototype;
GO

SET NOCOUNT ON;

PRINT N'A. VIEW EXISTENCE';
SELECT
    expected.ViewName,
    CONVERT(BIT, CASE WHEN actual.object_id IS NOT NULL THEN 1 ELSE 0 END) AS ViewExists
FROM (VALUES
    (N'vw_TripSummary'),
    (N'vw_TruckPerformance'),
    (N'vw_DriverPerformance'),
    (N'vw_StopAnalysis'),
    (N'vw_QualityAnalysis'),
    (N'vw_ExecutiveDashboard')
) AS expected (ViewName)
LEFT JOIN sys.views AS actual
    ON actual.name = expected.ViewName
    AND SCHEMA_NAME(actual.schema_id) = N'dbo'
ORDER BY expected.ViewName;

PRINT N'B. GRAIN COUNTS';
SELECT N'Trip / vw_TripSummary' AS CheckName,
       (SELECT COUNT_BIG(*) FROM dbo.Trip) AS SourceCount,
       (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary) AS ViewCount
UNION ALL
SELECT N'Truck / vw_TruckPerformance',
       (SELECT COUNT_BIG(*) FROM dbo.Truck),
       (SELECT COUNT_BIG(*) FROM dbo.vw_TruckPerformance)
UNION ALL
SELECT N'Driver / vw_DriverPerformance',
       (SELECT COUNT_BIG(*) FROM dbo.Driver),
       (SELECT COUNT_BIG(*) FROM dbo.vw_DriverPerformance)
UNION ALL
SELECT N'TruckStop / vw_StopAnalysis',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop),
       (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis)
UNION ALL
SELECT N'QualityResult / vw_QualityAnalysis',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult),
       (SELECT COUNT_BIG(*) FROM dbo.vw_QualityAnalysis)
UNION ALL
SELECT N'Executive expected row',
       CONVERT(BIGINT, 1),
       (SELECT COUNT_BIG(*) FROM dbo.vw_ExecutiveDashboard);

PRINT N'C. DUPLICATE IDENTIFIERS - EACH QUERY MUST RETURN ZERO ROWS';
SELECT TripID, COUNT_BIG(*) AS DuplicateCount
FROM dbo.vw_TripSummary
GROUP BY TripID
HAVING COUNT_BIG(*) > 1;

SELECT TruckID, COUNT_BIG(*) AS DuplicateCount
FROM dbo.vw_TruckPerformance
GROUP BY TruckID
HAVING COUNT_BIG(*) > 1;

SELECT DriverID, COUNT_BIG(*) AS DuplicateCount
FROM dbo.vw_DriverPerformance
GROUP BY DriverID
HAVING COUNT_BIG(*) > 1;

SELECT TruckStopID, COUNT_BIG(*) AS DuplicateCount
FROM dbo.vw_StopAnalysis
GROUP BY TruckStopID
HAVING COUNT_BIG(*) > 1;

SELECT QualityResultID, COUNT_BIG(*) AS DuplicateCount
FROM dbo.vw_QualityAnalysis
GROUP BY QualityResultID
HAVING COUNT_BIG(*) > 1;

PRINT N'D. TRIP RECONCILIATION';
SELECT N'TotalTrips' AS MetricName,
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip)) AS SourceValue,
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary)) AS ReportingValue
UNION ALL
SELECT N'CompletedTrips',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status = N'Completed')),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, IsCompleted)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'IncompleteTrips',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status <> N'Completed')),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, IsIncomplete)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'TotalTonnage',
       CONVERT(DECIMAL(38,3), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.Trip)),
       CONVERT(DECIMAL(38,3), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'AverageTripDurationMinutes',
       CONVERT(DECIMAL(38,3), (SELECT AVG(CONVERT(DECIMAL(18,3), DATEDIFF(MINUTE, StartTime, EndTime))) FROM dbo.Trip WHERE EndTime IS NOT NULL)),
       CONVERT(DECIMAL(38,3), (SELECT AVG(CONVERT(DECIMAL(18,3), TripDurationMinutes)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'TripsWithStops',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(DISTINCT TripID) FROM dbo.TruckStop WHERE TripID IS NOT NULL)),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, HasStops)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'TripsWithoutStops',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip) - (SELECT COUNT_BIG(DISTINCT TripID) FROM dbo.TruckStop WHERE TripID IS NOT NULL)),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasStops = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'TripsWithP2O5Results',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(DISTINCT TripID) FROM dbo.QualityResult WHERE TripID IS NOT NULL AND QualityUnit = N'P2O5Percent')),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, HasQualityResult)) FROM dbo.vw_TripSummary))
UNION ALL
SELECT N'TripsWithoutP2O5Results',
       CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip) - (SELECT COUNT_BIG(DISTINCT TripID) FROM dbo.QualityResult WHERE TripID IS NOT NULL AND QualityUnit = N'P2O5Percent')),
       CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasQualityResult = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_TripSummary));

PRINT N'E. TRUCK RECONCILIATION';
SELECT N'TotalTrips' AS MetricName,
       (SELECT COUNT_BIG(*) FROM dbo.Trip) AS SourceValue,
       (SELECT SUM(TotalTrips) FROM dbo.vw_TruckPerformance) AS ReportingValue
UNION ALL
SELECT N'CompletedTrips',
       (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status = N'Completed'),
       (SELECT SUM(CompletedTrips) FROM dbo.vw_TruckPerformance)
UNION ALL
SELECT N'IncompleteTrips',
       (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status <> N'Completed'),
       (SELECT SUM(IncompleteTrips) FROM dbo.vw_TruckPerformance)
UNION ALL
SELECT N'TotalStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop),
       (SELECT SUM(TotalStopCount) FROM dbo.vw_TruckPerformance)
UNION ALL
SELECT N'TotalDowntimeMinutes',
       (SELECT SUM(CONVERT(BIGINT, COALESCE(DurationMinutes, 0))) FROM dbo.TruckStop),
       (SELECT SUM(TotalDowntimeMinutes) FROM dbo.vw_TruckPerformance)
UNION ALL
SELECT N'LinkedQualityResults',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL),
       (SELECT SUM(QualityResultCount) FROM dbo.vw_TruckPerformance);

SELECT N'TotalTonnage' AS MetricName,
       CONVERT(DECIMAL(38,2), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.Trip)) AS SourceValue,
       CONVERT(DECIMAL(38,2), (SELECT SUM(TotalTonnage) FROM dbo.vw_TruckPerformance)) AS ReportingValue;

PRINT N'F. DRIVER RECONCILIATION';
SELECT N'TotalTrips' AS MetricName,
       (SELECT COUNT_BIG(*) FROM dbo.Trip) AS SourceValue,
       (SELECT SUM(TotalTrips) FROM dbo.vw_DriverPerformance) AS ReportingValue
UNION ALL
SELECT N'CompletedTrips',
       (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status = N'Completed'),
       (SELECT SUM(CompletedTrips) FROM dbo.vw_DriverPerformance)
UNION ALL
SELECT N'IncompleteTrips',
       (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status <> N'Completed'),
       (SELECT SUM(IncompleteTrips) FROM dbo.vw_DriverPerformance)
UNION ALL
SELECT N'LinkedStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NOT NULL),
       (SELECT SUM(TotalLinkedStopCount) FROM dbo.vw_DriverPerformance)
UNION ALL
SELECT N'LinkedQualityResults',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL),
       (SELECT SUM(QualityResultCount) FROM dbo.vw_DriverPerformance);

SELECT N'TotalTonnage' AS MetricName,
       CONVERT(DECIMAL(38,2), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.Trip)) AS SourceValue,
       CONVERT(DECIMAL(38,2), (SELECT SUM(TotalTonnage) FROM dbo.vw_DriverPerformance)) AS ReportingValue;

PRINT N'G. STOP RECONCILIATION';
SELECT N'TotalStops' AS MetricName,
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop) AS SourceValue,
       (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis) AS ReportingValue
UNION ALL
SELECT N'LinkedStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NOT NULL),
       (SELECT SUM(CONVERT(BIGINT, HasLinkedTrip)) FROM dbo.vw_StopAnalysis)
UNION ALL
SELECT N'UnlinkedStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NULL),
       (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasLinkedTrip = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_StopAnalysis)
UNION ALL
SELECT N'CompletedStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE EndTime IS NOT NULL AND EndTime >= StartTime),
       (SELECT SUM(CONVERT(BIGINT, IsCompletedStop)) FROM dbo.vw_StopAnalysis)
UNION ALL
SELECT N'OpenStops',
       (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE EndTime IS NULL),
       (SELECT SUM(CONVERT(BIGINT, IsOpenStop)) FROM dbo.vw_StopAnalysis);

SELECT TruckStopID, StopStartTime, StopEndTime, StopDurationMinutes
FROM dbo.vw_StopAnalysis
WHERE StopDurationMinutes < 0;

PRINT N'H. QUALITY RECONCILIATION';
SELECT N'TotalQualityResults' AS MetricName,
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult) AS SourceValue,
       (SELECT COUNT_BIG(*) FROM dbo.vw_QualityAnalysis) AS ReportingValue
UNION ALL
SELECT N'LinkedQualityResults',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL),
       (SELECT SUM(CONVERT(BIGINT, HasLinkedTrip)) FROM dbo.vw_QualityAnalysis)
UNION ALL
SELECT N'UnlinkedQualityResults',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NULL),
       (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasLinkedTrip = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_QualityAnalysis)
UNION ALL
SELECT N'P2O5Results',
       (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE QualityUnit = N'P2O5Percent'),
       (SELECT SUM(CONVERT(BIGINT, IsP2O5Measurement)) FROM dbo.vw_QualityAnalysis);

PRINT N'I. EXECUTIVE RECONCILIATION';
SELECT
    executive.TotalTrips,
    (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary) AS DirectTotalTrips,
    executive.TotalStops,
    (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis) AS DirectTotalStops,
    executive.TotalQualityResults,
    (SELECT COUNT_BIG(*) FROM dbo.vw_QualityAnalysis) AS DirectTotalQualityResults,
    executive.CompletedTrips + executive.IncompleteTrips AS RecombinedTrips,
    executive.TripsWithStops + executive.TripsWithoutStops AS RecombinedStopCoverage,
    executive.TripsWithQualityResults + executive.TripsWithoutQualityResults AS RecombinedQualityCoverage
FROM dbo.vw_ExecutiveDashboard AS executive;

PRINT N'J. CONSISTENCY ANOMALIES - EXPECTED RESULT IS ZERO ROWS';
SELECT N'vw_TripSummary' AS ObjectName, CONVERT(NVARCHAR(50), TripID) AS EntityID, N'Negative payload utilization or duration' AS Problem
FROM dbo.vw_TripSummary
WHERE PayloadUtilizationPercent < 0 OR TripDurationMinutes < 0
UNION ALL
SELECT N'vw_TruckPerformance', CONVERT(NVARCHAR(50), TruckID), N'Invalid completion rate or Has flag'
FROM dbo.vw_TruckPerformance
WHERE CompletionRatePercent NOT BETWEEN 0 AND 100
   OR TotalTrips < 0 OR CompletedTrips < 0 OR IncompleteTrips < 0 OR TotalStopCount < 0 OR QualityResultCount < 0
   OR HasTrips <> CONVERT(BIT, CASE WHEN TotalTrips > 0 THEN 1 ELSE 0 END)
   OR HasStops <> CONVERT(BIT, CASE WHEN TotalStopCount > 0 THEN 1 ELSE 0 END)
   OR HasQualityResults <> CONVERT(BIT, CASE WHEN QualityResultCount > 0 THEN 1 ELSE 0 END)
   OR CompletedTrips + IncompleteTrips <> TotalTrips
UNION ALL
SELECT N'vw_DriverPerformance', CONVERT(NVARCHAR(50), DriverID), N'Invalid completion rate or Has flag'
FROM dbo.vw_DriverPerformance
WHERE CompletionRatePercent NOT BETWEEN 0 AND 100
   OR TotalTrips < 0 OR CompletedTrips < 0 OR IncompleteTrips < 0 OR TotalLinkedStopCount < 0 OR QualityResultCount < 0
   OR HasTrips <> CONVERT(BIT, CASE WHEN TotalTrips > 0 THEN 1 ELSE 0 END)
   OR HasLinkedStops <> CONVERT(BIT, CASE WHEN TotalLinkedStopCount > 0 THEN 1 ELSE 0 END)
   OR HasQualityResults <> CONVERT(BIT, CASE WHEN QualityResultCount > 0 THEN 1 ELSE 0 END)
   OR CompletedTrips + IncompleteTrips <> TotalTrips
UNION ALL
SELECT N'vw_StopAnalysis', CONVERT(NVARCHAR(50), TruckStopID), N'Negative stop duration'
FROM dbo.vw_StopAnalysis
WHERE StopDurationMinutes < 0
UNION ALL
SELECT N'vw_ExecutiveDashboard', N'GLOBAL', N'Executive totals do not recombine'
FROM dbo.vw_ExecutiveDashboard
WHERE CompletionRatePercent NOT BETWEEN 0 AND 100
   OR CompletedTrips + IncompleteTrips <> TotalTrips
   OR TripsWithStops + TripsWithoutStops <> TotalTrips
   OR TripsWithQualityResults + TripsWithoutQualityResults <> TotalTrips;

PRINT N'K. FINAL PASS/FAIL SUMMARY';
WITH Checks AS (
    SELECT N'View exists: vw_TripSummary' AS CheckName,
           CONVERT(DECIMAL(38,3), 1) AS ExpectedValue,
           CONVERT(DECIMAL(38,3), CASE WHEN OBJECT_ID(N'dbo.vw_TripSummary', N'V') IS NULL THEN 0 ELSE 1 END) AS ActualValue
    UNION ALL SELECT N'View exists: vw_TruckPerformance', 1, CASE WHEN OBJECT_ID(N'dbo.vw_TruckPerformance', N'V') IS NULL THEN 0 ELSE 1 END
    UNION ALL SELECT N'View exists: vw_DriverPerformance', 1, CASE WHEN OBJECT_ID(N'dbo.vw_DriverPerformance', N'V') IS NULL THEN 0 ELSE 1 END
    UNION ALL SELECT N'View exists: vw_StopAnalysis', 1, CASE WHEN OBJECT_ID(N'dbo.vw_StopAnalysis', N'V') IS NULL THEN 0 ELSE 1 END
    UNION ALL SELECT N'View exists: vw_QualityAnalysis', 1, CASE WHEN OBJECT_ID(N'dbo.vw_QualityAnalysis', N'V') IS NULL THEN 0 ELSE 1 END
    UNION ALL SELECT N'View exists: vw_ExecutiveDashboard', 1, CASE WHEN OBJECT_ID(N'dbo.vw_ExecutiveDashboard', N'V') IS NULL THEN 0 ELSE 1 END
    UNION ALL SELECT N'Trip grain count', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip)), CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary))
    UNION ALL SELECT N'Truck grain count', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Truck)), CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Driver grain count', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Driver)), CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Stop grain count', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop)), CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis))
    UNION ALL SELECT N'Quality grain count', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult)), CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_QualityAnalysis))
    UNION ALL SELECT N'Executive grain count', 1, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Trip duplicate groups', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM (SELECT TripID FROM dbo.vw_TripSummary GROUP BY TripID HAVING COUNT_BIG(*) > 1) AS duplicates))
    UNION ALL SELECT N'Truck duplicate groups', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM (SELECT TruckID FROM dbo.vw_TruckPerformance GROUP BY TruckID HAVING COUNT_BIG(*) > 1) AS duplicates))
    UNION ALL SELECT N'Driver duplicate groups', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM (SELECT DriverID FROM dbo.vw_DriverPerformance GROUP BY DriverID HAVING COUNT_BIG(*) > 1) AS duplicates))
    UNION ALL SELECT N'Stop duplicate groups', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM (SELECT TruckStopID FROM dbo.vw_StopAnalysis GROUP BY TruckStopID HAVING COUNT_BIG(*) > 1) AS duplicates))
    UNION ALL SELECT N'Quality duplicate groups', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM (SELECT QualityResultID FROM dbo.vw_QualityAnalysis GROUP BY QualityResultID HAVING COUNT_BIG(*) > 1) AS duplicates))
    UNION ALL SELECT N'Truck trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalTrips) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck completed-trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status = N'Completed')), CONVERT(DECIMAL(38,3), (SELECT SUM(CompletedTrips) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck incomplete-trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status <> N'Completed')), CONVERT(DECIMAL(38,3), (SELECT SUM(IncompleteTrips) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck tonnage reconciliation', CONVERT(DECIMAL(38,3), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.Trip)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalTonnage) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck stop reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalStopCount) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck downtime reconciliation', CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, COALESCE(DurationMinutes, 0))) FROM dbo.TruckStop)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalDowntimeMinutes) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Truck quality reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(QualityResultCount) FROM dbo.vw_TruckPerformance))
    UNION ALL SELECT N'Driver trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalTrips) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Driver completed-trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status = N'Completed')), CONVERT(DECIMAL(38,3), (SELECT SUM(CompletedTrips) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Driver incomplete-trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.Trip WHERE Status <> N'Completed')), CONVERT(DECIMAL(38,3), (SELECT SUM(IncompleteTrips) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Driver tonnage reconciliation', CONVERT(DECIMAL(38,3), (SELECT SUM(COALESCE(Tonnage, 0)) FROM dbo.Trip)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalTonnage) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Driver linked-stop reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NOT NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(TotalLinkedStopCount) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Driver quality reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(QualityResultCount) FROM dbo.vw_DriverPerformance))
    UNION ALL SELECT N'Stop linked reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NOT NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, HasLinkedTrip)) FROM dbo.vw_StopAnalysis))
    UNION ALL SELECT N'Stop unlinked reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE TripID IS NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasLinkedTrip = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_StopAnalysis))
    UNION ALL SELECT N'Stop completed reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE EndTime IS NOT NULL AND EndTime >= StartTime)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, IsCompletedStop)) FROM dbo.vw_StopAnalysis))
    UNION ALL SELECT N'Stop open reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.TruckStop WHERE EndTime IS NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, IsOpenStop)) FROM dbo.vw_StopAnalysis))
    UNION ALL SELECT N'Quality linked reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NOT NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, HasLinkedTrip)) FROM dbo.vw_QualityAnalysis))
    UNION ALL SELECT N'Quality unlinked reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE TripID IS NULL)), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, CASE WHEN HasLinkedTrip = 0 THEN 1 ELSE 0 END)) FROM dbo.vw_QualityAnalysis))
    UNION ALL SELECT N'Quality P2O5 reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.QualityResult WHERE QualityUnit = N'P2O5Percent')), CONVERT(DECIMAL(38,3), (SELECT SUM(CONVERT(BIGINT, IsP2O5Measurement)) FROM dbo.vw_QualityAnalysis))
    UNION ALL SELECT N'Executive trip reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary)), CONVERT(DECIMAL(38,3), (SELECT TotalTrips FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Executive stop reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis)), CONVERT(DECIMAL(38,3), (SELECT TotalStops FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Executive quality reconciliation', CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_QualityAnalysis)), CONVERT(DECIMAL(38,3), (SELECT TotalQualityResults FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Executive completed plus incomplete', CONVERT(DECIMAL(38,3), (SELECT TotalTrips FROM dbo.vw_ExecutiveDashboard)), CONVERT(DECIMAL(38,3), (SELECT CompletedTrips + IncompleteTrips FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Executive stop coverage', CONVERT(DECIMAL(38,3), (SELECT TotalTrips FROM dbo.vw_ExecutiveDashboard)), CONVERT(DECIMAL(38,3), (SELECT TripsWithStops + TripsWithoutStops FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Executive quality coverage', CONVERT(DECIMAL(38,3), (SELECT TotalTrips FROM dbo.vw_ExecutiveDashboard)), CONVERT(DECIMAL(38,3), (SELECT TripsWithQualityResults + TripsWithoutQualityResults FROM dbo.vw_ExecutiveDashboard))
    UNION ALL SELECT N'Negative trip KPI anomalies', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_TripSummary WHERE PayloadUtilizationPercent < 0 OR TripDurationMinutes < 0))
    UNION ALL SELECT N'Negative stop-duration anomalies', 0, CONVERT(DECIMAL(38,3), (SELECT COUNT_BIG(*) FROM dbo.vw_StopAnalysis WHERE StopDurationMinutes < 0))
)
SELECT
    CheckName,
    ExpectedValue,
    ActualValue,
    CONVERT(NVARCHAR(4), CASE WHEN ExpectedValue = ActualValue THEN N'PASS' ELSE N'FAIL' END) AS ValidationStatus
FROM Checks
ORDER BY CheckName;
GO
