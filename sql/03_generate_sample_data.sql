/*
    Script 03 - Generate deterministic sample data

    Run after:
    1. sql/01_create_database.sql
    2. sql/02_seed_reference_data.sql

    The generated data is fictional and deterministic. It contains a small
    number of controlled anomalies for validation practice without breaking
    database constraints.
*/

USE OCP_Mining_Prototype;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF (SELECT COUNT(*) FROM dbo.Truck) < 12
    BEGIN
        ;THROW 51001, N'Reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.Driver) < 20
    BEGIN
        ;THROW 51002, N'Driver reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.WorkShift) < 3
    BEGIN
        ;THROW 51003, N'Shift reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.LoadingPoint) < 4
    BEGIN
        ;THROW 51004, N'Loading point reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.Destination) < 3
    BEGIN
        ;THROW 51005, N'Destination reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.StopReason) < 8
    BEGIN
        ;THROW 51006, N'Stop reason reference data is incomplete. Run sql/02_seed_reference_data.sql first.', 1;
    END;

    DELETE FROM dbo.QualityResult;
    DELETE FROM dbo.TruckStop;
    DELETE FROM dbo.Trip;

    DBCC CHECKIDENT ('dbo.QualityResult', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.TruckStop', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Trip', RESEED, 0) WITH NO_INFOMSGS;

    DECLARE @StartDate DATE = DATEFROMPARTS(2026, 6, 1);
    DECLARE @TripCount INT = 800;
    DECLARE @StopCount INT = 150;
    DECLARE @QualityCount INT = 120;

    ;WITH Numbers AS (
        SELECT 1 AS N
        UNION ALL
        SELECT N + 1
        FROM Numbers
        WHERE N < @TripCount
    ),
    DayPlan AS (
        SELECT
            DayIndex,
            DailyTripCount
        FROM (VALUES
            (1, 24), (2, 29), (3, 25), (4, 28), (5, 23),
            (6, 31), (7, 26), (8, 30), (9, 24), (10, 28),
            (11, 25), (12, 29), (13, 26), (14, 27), (15, 24),
            (16, 30), (17, 25), (18, 28), (19, 26), (20, 31),
            (21, 23), (22, 29), (23, 25), (24, 28), (25, 24),
            (26, 30), (27, 26), (28, 27), (29, 25), (30, 24)
        ) AS source (DayIndex, DailyTripCount)
    ),
    DayBoundaries AS (
        SELECT
            DayIndex,
            DailyTripCount,
            SUM(DailyTripCount) OVER (ORDER BY DayIndex) AS CumulativeTripCount,
            SUM(DailyTripCount) OVER (ORDER BY DayIndex) - DailyTripCount AS PreviousCumulativeTripCount
        FROM DayPlan
    ),
    PlannedTrips AS (
        SELECT
            numbers.N,
            TripDate = DATEADD(DAY, dayPlan.DayIndex - 1, @StartDate),
            DayTripSequence = numbers.N - dayPlan.PreviousCumulativeTripCount,
            ShiftCode =
                CASE
                    WHEN (numbers.N * 37) % 100 < 36 THEN N'SHIFT-A'
                    WHEN (numbers.N * 37) % 100 < 70 THEN N'SHIFT-B'
                    ELSE N'SHIFT-C'
                END,
            TruckCode =
                CASE
                    WHEN (numbers.N * 19) % 100 < 9 THEN N'TRK-001'
                    WHEN (numbers.N * 19) % 100 < 17 THEN N'TRK-002'
                    WHEN (numbers.N * 19) % 100 < 25 THEN N'TRK-003'
                    WHEN (numbers.N * 19) % 100 < 32 THEN N'TRK-004'
                    WHEN (numbers.N * 19) % 100 < 40 THEN N'TRK-005'
                    WHEN (numbers.N * 19) % 100 < 47 THEN N'TRK-006'
                    WHEN (numbers.N * 19) % 100 < 56 THEN N'TRK-007'
                    WHEN (numbers.N * 19) % 100 < 64 THEN N'TRK-008'
                    WHEN (numbers.N * 19) % 100 < 72 THEN N'TRK-009'
                    WHEN (numbers.N * 19) % 100 < 80 THEN N'TRK-010'
                    WHEN (numbers.N * 19) % 100 < 90 THEN N'TRK-011'
                    ELSE N'TRK-012'
                END,
            DriverCode = CONCAT(N'DRV-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), ((numbers.N * 11 + ((numbers.N - 1) / 23)) % 20) + 1)), 3)),
            LoadingPointCode =
                CASE ((numbers.N * 3) % 4) + 1
                    WHEN 1 THEN N'LP-01'
                    WHEN 2 THEN N'LP-02'
                    WHEN 3 THEN N'LP-03'
                    ELSE N'LP-04'
                END,
            DestinationCode =
                CASE
                    WHEN (numbers.N * 17) % 100 < 62 THEN N'DEST-PROCESS'
                    WHEN (numbers.N * 17) % 100 < 87 THEN N'DEST-STOCK'
                    ELSE N'DEST-DUMP'
                END,
            ShiftStartMinute =
                CASE
                    WHEN (numbers.N * 37) % 100 < 36 THEN 6 * 60
                    WHEN (numbers.N * 37) % 100 < 70 THEN 14 * 60
                    ELSE 22 * 60
                END,
            StartOffsetMinutes = ((numbers.N - dayPlan.PreviousCumulativeTripCount - 1) * 14) + ((numbers.N * 3) % 7),
            TripDurationMinutes =
                CASE
                    WHEN numbers.N % 10 = 0 THEN 31 + (((numbers.N / 10) * 7) % 20)
                    ELSE 18 + ((numbers.N * 11) % 13)
                END,
            TripStatus =
                CASE
                    WHEN numbers.N % 157 = 0 THEN N'InProgress'
                    WHEN numbers.N % 61 = 0 THEN N'Incomplete'
                    ELSE N'Completed'
                END
        FROM Numbers AS numbers
        INNER JOIN DayBoundaries AS dayPlan
            ON numbers.N > dayPlan.PreviousCumulativeTripCount
            AND numbers.N <= dayPlan.CumulativeTripCount
    )
    INSERT INTO dbo.Trip (
        TripDate,
        ShiftID,
        TruckID,
        DriverID,
        LoadingPointID,
        DestinationID,
        StartTime,
        EndTime,
        Tonnage,
        Status,
        Comments
    )
    SELECT
        pt.TripDate,
        ws.ShiftID,
        tr.TruckID,
        dr.DriverID,
        lp.LoadingPointID,
        dest.DestinationID,
        DATEADD(MINUTE, pt.ShiftStartMinute + pt.StartOffsetMinutes, CAST(pt.TripDate AS DATETIME2(0))) AS StartTime,
        CASE
            WHEN pt.TripStatus = N'Completed' THEN
                DATEADD(MINUTE, pt.ShiftStartMinute + pt.StartOffsetMinutes + pt.TripDurationMinutes, CAST(pt.TripDate AS DATETIME2(0)))
            ELSE NULL
        END AS EndTime,
        CASE
            WHEN pt.TripStatus <> N'Completed' THEN NULL
            WHEN tr.RatedPayloadTonnes = 55.00 THEN
                CONVERT(DECIMAL(10,2), 50.00 + (((pt.N * 7) % 51) / 10.0))
            WHEN pt.N % 43 = 0 THEN
                CONVERT(DECIMAL(10,2), tr.RatedPayloadTonnes * (0.72 + (((pt.N * 5) % 14) / 100.0)))
            ELSE
                CONVERT(DECIMAL(10,2), tr.RatedPayloadTonnes * (0.85 + (((pt.N * 7) % 16) / 100.0)))
        END AS Tonnage,
        pt.TripStatus,
        CASE
            WHEN pt.TripStatus <> N'Completed' THEN CONCAT(N'Partial analytical sample - generated ', pt.TripStatus, N' trip.')
            WHEN pt.TripDurationMinutes > 30 THEN N'Analytical long trip in synthetic partial sample.'
            WHEN pt.N % 43 = 0 AND tr.RatedPayloadTonnes > 55.00 THEN N'Controlled underload case for dashboard validation.'
            ELSE NULL
        END AS Comments
    FROM PlannedTrips AS pt
    INNER JOIN dbo.WorkShift AS ws
        ON ws.ShiftCode = pt.ShiftCode
    INNER JOIN dbo.Truck AS tr
        ON tr.TruckCode = pt.TruckCode
    INNER JOIN dbo.Driver AS dr
        ON dr.DriverCode = pt.DriverCode
    INNER JOIN dbo.LoadingPoint AS lp
        ON lp.LoadingPointCode = pt.LoadingPointCode
    INNER JOIN dbo.Destination AS dest
        ON dest.DestinationCode = pt.DestinationCode
    OPTION (MAXRECURSION 0);

    ;WITH Numbers AS (
        SELECT 1 AS N
        UNION ALL
        SELECT N + 1
        FROM Numbers
        WHERE N < @StopCount
    ),
    AvailableTrips AS (
        SELECT
            TripID,
            TruckID,
            DriverID,
            ShiftID,
            StartTime,
            ROW_NUMBER() OVER (ORDER BY TripID) AS TripSequence
        FROM dbo.Trip
    ),
    PlannedStops AS (
        SELECT
            N,
            FallbackTripSequence = ((N * 17) % @TripCount) + 1,
            LinkedTripSequence =
                CASE
                    WHEN N % 5 = 0 THEN NULL
                    ELSE ((N * 17) % @TripCount) + 1
                END,
            FallbackTruckCode = CONCAT(N'TRK-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), ((N * 5) % 12) + 1)), 3)),
            ReasonCode =
                CASE
                    WHEN N <= 38 THEN N'WAIT_LOADING'
                    WHEN N <= 61 THEN N'WAIT_UNLOADING'
                    WHEN N <= 83 THEN N'PREVENTIVE_MAINTENANCE'
                    WHEN N <= 98 THEN N'TIRE_FAILURE'
                    WHEN N <= 109 THEN N'BRAKE_FAILURE'
                    WHEN N <= 124 THEN N'HYDRAULIC_FAILURE'
                    WHEN N <= 136 THEN N'ELECTRICAL_FAILURE'
                    ELSE N'ROAD_OR_SAFETY_DELAY'
                END,
            StopOffsetMinutes = 5 + ((N * 11) % 120)
        FROM Numbers
    ),
    PlannedStopsWithDurations AS (
        SELECT
            N,
            FallbackTripSequence,
            LinkedTripSequence,
            FallbackTruckCode,
            ReasonCode,
            StopOffsetMinutes,
            StopDurationMinutes =
                CASE ReasonCode
                    WHEN N'TIRE_FAILURE' THEN 240 + ((N * 17) % 241)
                    WHEN N'BRAKE_FAILURE' THEN 360 + ((N * 23) % 241)
                    WHEN N'HYDRAULIC_FAILURE' THEN 240 + ((N * 19) % 301)
                    WHEN N'ELECTRICAL_FAILURE' THEN 60 + ((N * 31) % 421)
                    WHEN N'PREVENTIVE_MAINTENANCE' THEN
                        CASE
                            WHEN N % 11 = 0 THEN 240 + ((N * 29) % 481)
                            ELSE 30 + ((N * 17) % 211)
                        END
                    WHEN N'WAIT_LOADING' THEN 1 + ((N * 5) % 12)
                    WHEN N'WAIT_UNLOADING' THEN 1 + ((N * 3) % 8)
                    ELSE 10 + ((N * 13) % 111)
                END
        FROM PlannedStops
    )
    INSERT INTO dbo.TruckStop (
        TruckID,
        DriverID,
        ShiftID,
        TripID,
        StopReasonID,
        StartTime,
        EndTime,
        ValidationStatus,
        Comments
    )
    SELECT
        CASE
            WHEN ps.LinkedTripSequence IS NULL THEN fallbackTruck.TruckID
            ELSE tripForTime.TruckID
        END AS TruckID,
        CASE
            WHEN ps.LinkedTripSequence IS NULL OR ps.N % 7 = 0 THEN NULL
            ELSE tripForTime.DriverID
        END AS DriverID,
        CASE
            WHEN ps.LinkedTripSequence IS NULL OR ps.N % 6 = 0 THEN NULL
            ELSE tripForTime.ShiftID
        END AS ShiftID,
        CASE
            WHEN ps.LinkedTripSequence IS NULL THEN NULL
            ELSE tripForTime.TripID
        END AS TripID,
        sr.StopReasonID,
        DATEADD(MINUTE, ps.StopOffsetMinutes, tripForTime.StartTime) AS StartTime,
        DATEADD(MINUTE, ps.StopOffsetMinutes + ps.StopDurationMinutes, tripForTime.StartTime) AS EndTime,
        N'Validated' AS ValidationStatus,
        CASE
            WHEN ps.ReasonCode IN (N'TIRE_FAILURE', N'BRAKE_FAILURE', N'HYDRAULIC_FAILURE', N'ELECTRICAL_FAILURE') THEN N'Benchmark-informed synthetic mechanical downtime.'
            WHEN ps.ReasonCode = N'PREVENTIVE_MAINTENANCE' THEN N'Planned synthetic preventive maintenance event.'
            WHEN ps.StopDurationMinutes >= 180 THEN N'Controlled anomaly: long downtime event.'
            WHEN ps.StopDurationMinutes <= 10 THEN N'Controlled anomaly: very short downtime event.'
            ELSE NULL
        END AS Comments
    FROM PlannedStopsWithDurations AS ps
    INNER JOIN AvailableTrips AS tripForTime
        ON tripForTime.TripSequence = ps.FallbackTripSequence
    INNER JOIN dbo.Truck AS fallbackTruck
        ON fallbackTruck.TruckCode = ps.FallbackTruckCode
    INNER JOIN dbo.StopReason AS sr
        ON sr.ReasonCode = ps.ReasonCode
    OPTION (MAXRECURSION 0);

    ;WITH Numbers AS (
        SELECT 1 AS N
        UNION ALL
        SELECT N + 1
        FROM Numbers
        WHERE N < @QualityCount
    ),
    PlannedQuality AS (
        SELECT
            N,
            FallbackTripID = CONVERT(BIGINT, ((N * 29) % @TripCount) + 1),
            LinkedTripID =
                CASE
                    WHEN N % 6 = 0 THEN NULL
                    ELSE CONVERT(BIGINT, ((N * 29) % @TripCount) + 1)
                END,
            LoadingPointCode =
                CASE ((N * 5) % 4) + 1
                    WHEN 1 THEN N'LP-01'
                    WHEN 2 THEN N'LP-02'
                    WHEN 3 THEN N'LP-03'
                    ELSE N'LP-04'
                END,
            AnalysisDate = DATEADD(MINUTE, (N * 43) % 1440, CAST(DATEADD(DAY, (N - 1) % 30, @StartDate) AS DATETIME2(0))),
            QualityValue = CONVERT(DECIMAL(10,3), 28.500 + (((N * 17) % 301) / 100.0))
        FROM Numbers
    )
    INSERT INTO dbo.QualityResult (
        AnalysisDate,
        TripID,
        LoadingPointID,
        QualityValue,
        QualityUnit,
        TargetValue,
        Status,
        Comments
    )
    SELECT
        pq.AnalysisDate,
        pq.LinkedTripID,
        CASE
            WHEN pq.LinkedTripID IS NULL THEN lp.LoadingPointID
            WHEN pq.N % 4 = 0 THEN NULL
            ELSE tripForQuality.LoadingPointID
        END AS LoadingPointID,
        pq.QualityValue,
        N'P2O5Percent' AS QualityUnit,
        CONVERT(DECIMAL(10,3), 30.000) AS TargetValue,
        CASE
            WHEN pq.N % 37 = 0 THEN N'Review'
            WHEN pq.QualityValue < 29.000 THEN N'BelowTarget'
            WHEN pq.QualityValue > 31.000 THEN N'AboveTarget'
            ELSE N'InTarget'
        END AS Status,
        CASE
            WHEN pq.N % 37 = 0 THEN N'Prototype P2O5 result marked for review; thresholds are not official.'
            WHEN pq.QualityValue < 29.000 THEN N'Prototype low P2O5 band; threshold is not official.'
            WHEN pq.QualityValue > 31.000 THEN N'Prototype high P2O5 band; threshold is not official.'
            ELSE NULL
        END AS Comments
    FROM PlannedQuality AS pq
    INNER JOIN dbo.Trip AS tripForQuality
        ON tripForQuality.TripID = pq.FallbackTripID
    INNER JOIN dbo.LoadingPoint AS lp
        ON lp.LoadingPointCode = pq.LoadingPointCode
    OPTION (MAXRECURSION 0);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

SELECT N'Truck' AS ObjectName, COUNT(*) AS GeneratedCount FROM dbo.Truck
UNION ALL SELECT N'Driver', COUNT(*) FROM dbo.Driver
UNION ALL SELECT N'WorkShift', COUNT(*) FROM dbo.WorkShift
UNION ALL SELECT N'LoadingPoint', COUNT(*) FROM dbo.LoadingPoint
UNION ALL SELECT N'Destination', COUNT(*) FROM dbo.Destination
UNION ALL SELECT N'StopReason', COUNT(*) FROM dbo.StopReason
UNION ALL SELECT N'Trip', COUNT(*) FROM dbo.Trip
UNION ALL SELECT N'TruckStop', COUNT(*) FROM dbo.TruckStop
UNION ALL SELECT N'QualityResult', COUNT(*) FROM dbo.QualityResult;
GO
