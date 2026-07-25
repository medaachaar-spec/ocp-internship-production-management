-- Read-only Sprint 0 verification script.
SELECT DB_NAME() AS CurrentDatabase, @@SERVERNAME AS SqlServerName;

SELECT s.name AS SchemaName, o.name AS ObjectName, o.type_desc AS ObjectType
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.name IN (
    'Truck', 'Driver', 'WorkShift', 'LoadingPoint', 'Destination', 'StopReason',
    'Trip', 'TruckStop', 'QualityResult',
    'vw_TripSummary', 'vw_TruckPerformance', 'vw_DriverPerformance',
    'vw_StopAnalysis', 'vw_QualityAnalysis', 'vw_TripDetails',
    'vw_TripsDailySummary', 'vw_TruckStopDetails', 'vw_StopsByReason',
    'vw_QualityByPeriod', 'vw_GlobalProductionSummary', 'vw_ExecutiveDashboard'
)
ORDER BY o.type_desc, s.name, o.name;
