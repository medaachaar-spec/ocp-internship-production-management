/*
    Script 01 - Create prototype database schema
    Project: Mining production management and reporting system
    Environment: Microsoft SQL Server

    This script creates or resets the prototype schema for the MVP phase.
    It uses fictional structure only and does not contain real OCP data.
*/

SET NOCOUNT ON;

IF DB_ID(N'OCP_Mining_Prototype') IS NULL
BEGIN
    CREATE DATABASE OCP_Mining_Prototype;
END;
GO

USE OCP_Mining_Prototype;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DROP VIEW IF EXISTS dbo.vw_QualityByPeriod;
    DROP VIEW IF EXISTS dbo.vw_GlobalProductionSummary;
    DROP VIEW IF EXISTS dbo.vw_StopsByReason;
    DROP VIEW IF EXISTS dbo.vw_TripsDailySummary;
    DROP VIEW IF EXISTS dbo.vw_DriverPerformance;
    DROP VIEW IF EXISTS dbo.vw_TruckPerformance;
    DROP VIEW IF EXISTS dbo.vw_TruckStopDetails;
    DROP VIEW IF EXISTS dbo.vw_TripDetails;

    DROP TABLE IF EXISTS dbo.QualityResult;
    DROP TABLE IF EXISTS dbo.TruckStop;
    DROP TABLE IF EXISTS dbo.Trip;
    DROP TABLE IF EXISTS dbo.StopReason;
    DROP TABLE IF EXISTS dbo.Destination;
    DROP TABLE IF EXISTS dbo.LoadingPoint;
    DROP TABLE IF EXISTS dbo.WorkShift;
    DROP TABLE IF EXISTS dbo.Driver;
    DROP TABLE IF EXISTS dbo.Truck;

    CREATE TABLE dbo.Truck (
        TruckID              INT IDENTITY(1,1) NOT NULL,
        TruckCode            NVARCHAR(20) NOT NULL,
        RegistrationNumber   NVARCHAR(30) NULL,
        Model                NVARCHAR(80) NULL,
        RatedPayloadTonnes   DECIMAL(10,2) NULL,
        Status               NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Truck_Status DEFAULT N'Available',
        IsActive             BIT NOT NULL
            CONSTRAINT DF_Truck_IsActive DEFAULT (1),
        CreatedAt            DATETIME2(0) NOT NULL
            CONSTRAINT DF_Truck_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Truck PRIMARY KEY CLUSTERED (TruckID),
        CONSTRAINT UQ_Truck_TruckCode UNIQUE (TruckCode),
        CONSTRAINT CK_Truck_Status
            CHECK (Status IN (N'Available', N'InService', N'Maintenance', N'OutOfService')),
        CONSTRAINT CK_Truck_RatedPayloadTonnes
            CHECK (RatedPayloadTonnes IS NULL OR RatedPayloadTonnes > 0)
    );

    CREATE TABLE dbo.Driver (
        DriverID             INT IDENTITY(1,1) NOT NULL,
        DriverCode           NVARCHAR(20) NOT NULL,
        FullName             NVARCHAR(120) NOT NULL,
        LicenseNumber        NVARCHAR(50) NULL,
        Status               NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Driver_Status DEFAULT N'Active',
        IsActive             BIT NOT NULL
            CONSTRAINT DF_Driver_IsActive DEFAULT (1),
        CreatedAt            DATETIME2(0) NOT NULL
            CONSTRAINT DF_Driver_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Driver PRIMARY KEY CLUSTERED (DriverID),
        CONSTRAINT UQ_Driver_DriverCode UNIQUE (DriverCode),
        CONSTRAINT CK_Driver_Status
            CHECK (Status IN (N'Active', N'Inactive', N'OnLeave'))
    );

    CREATE TABLE dbo.WorkShift (
        ShiftID              INT IDENTITY(1,1) NOT NULL,
        ShiftCode            NVARCHAR(20) NOT NULL,
        ShiftName            NVARCHAR(50) NOT NULL,
        StartTime            TIME(0) NOT NULL,
        EndTime              TIME(0) NOT NULL,
        IsActive             BIT NOT NULL
            CONSTRAINT DF_WorkShift_IsActive DEFAULT (1),

        CONSTRAINT PK_WorkShift PRIMARY KEY CLUSTERED (ShiftID),
        CONSTRAINT UQ_WorkShift_ShiftCode UNIQUE (ShiftCode)
    );

    CREATE TABLE dbo.LoadingPoint (
        LoadingPointID       INT IDENTITY(1,1) NOT NULL,
        LoadingPointCode     NVARCHAR(20) NOT NULL,
        LoadingPointName     NVARCHAR(100) NOT NULL,
        ZoneName             NVARCHAR(100) NULL,
        LoadingPointDescription NVARCHAR(250) NULL,
        IsActive             BIT NOT NULL
            CONSTRAINT DF_LoadingPoint_IsActive DEFAULT (1),

        CONSTRAINT PK_LoadingPoint PRIMARY KEY CLUSTERED (LoadingPointID),
        CONSTRAINT UQ_LoadingPoint_LoadingPointCode UNIQUE (LoadingPointCode)
    );

    CREATE TABLE dbo.Destination (
        DestinationID        INT IDENTITY(1,1) NOT NULL,
        DestinationCode      NVARCHAR(20) NOT NULL,
        DestinationName      NVARCHAR(100) NOT NULL,
        DestinationType      NVARCHAR(30) NULL,
        IsActive             BIT NOT NULL
            CONSTRAINT DF_Destination_IsActive DEFAULT (1),

        CONSTRAINT PK_Destination PRIMARY KEY CLUSTERED (DestinationID),
        CONSTRAINT UQ_Destination_DestinationCode UNIQUE (DestinationCode),
        CONSTRAINT CK_Destination_DestinationType
            CHECK (DestinationType IS NULL OR DestinationType IN (N'Plant', N'Stockpile', N'Waste', N'Other'))
    );

    CREATE TABLE dbo.StopReason (
        StopReasonID         INT IDENTITY(1,1) NOT NULL,
        ReasonCode           NVARCHAR(30) NOT NULL,
        ReasonName           NVARCHAR(100) NOT NULL,
        Category             NVARCHAR(40) NULL,
        IsPlanned            BIT NOT NULL
            CONSTRAINT DF_StopReason_IsPlanned DEFAULT (0),
        IsActive             BIT NOT NULL
            CONSTRAINT DF_StopReason_IsActive DEFAULT (1),

        CONSTRAINT PK_StopReason PRIMARY KEY CLUSTERED (StopReasonID),
        CONSTRAINT UQ_StopReason_ReasonCode UNIQUE (ReasonCode),
        CONSTRAINT CK_StopReason_Category
            CHECK (Category IS NULL OR Category IN (N'Mechanical', N'Maintenance', N'Operational'))
    );

    CREATE TABLE dbo.Trip (
        TripID               BIGINT IDENTITY(1,1) NOT NULL,
        TripDate             DATE NOT NULL,
        ShiftID              INT NOT NULL,
        TruckID              INT NOT NULL,
        DriverID             INT NOT NULL,
        LoadingPointID       INT NOT NULL,
        DestinationID        INT NOT NULL,
        StartTime            DATETIME2(0) NOT NULL,
        EndTime              DATETIME2(0) NULL,
        Tonnage              DECIMAL(10,2) NULL,
        Status               NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Trip_Status DEFAULT N'Completed',
        Comments             NVARCHAR(500) NULL,
        CreatedAt            DATETIME2(0) NOT NULL
            CONSTRAINT DF_Trip_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Trip PRIMARY KEY CLUSTERED (TripID),
        CONSTRAINT FK_Trip_WorkShift FOREIGN KEY (ShiftID)
            REFERENCES dbo.WorkShift(ShiftID),
        CONSTRAINT FK_Trip_Truck FOREIGN KEY (TruckID)
            REFERENCES dbo.Truck(TruckID),
        CONSTRAINT FK_Trip_Driver FOREIGN KEY (DriverID)
            REFERENCES dbo.Driver(DriverID),
        CONSTRAINT FK_Trip_LoadingPoint FOREIGN KEY (LoadingPointID)
            REFERENCES dbo.LoadingPoint(LoadingPointID),
        CONSTRAINT FK_Trip_Destination FOREIGN KEY (DestinationID)
            REFERENCES dbo.Destination(DestinationID),
        CONSTRAINT CK_Trip_Status
            CHECK (Status IN (N'Planned', N'InProgress', N'Completed', N'Cancelled', N'Incomplete')),
        CONSTRAINT CK_Trip_EndTime
            CHECK (EndTime IS NULL OR EndTime >= StartTime),
        CONSTRAINT CK_Trip_Tonnage
            CHECK (Tonnage IS NULL OR Tonnage >= 0)
    );

    CREATE TABLE dbo.TruckStop (
        TruckStopID          BIGINT IDENTITY(1,1) NOT NULL,
        TruckID              INT NOT NULL,
        DriverID             INT NULL,
        ShiftID              INT NULL,
        TripID               BIGINT NULL,
        StopReasonID         INT NOT NULL,
        StartTime            DATETIME2(0) NOT NULL,
        EndTime              DATETIME2(0) NULL,
        DurationMinutes AS
            (CASE
                WHEN EndTime IS NULL THEN NULL
                ELSE DATEDIFF(MINUTE, StartTime, EndTime)
             END) PERSISTED,
        ValidationStatus     NVARCHAR(20) NOT NULL
            CONSTRAINT DF_TruckStop_ValidationStatus DEFAULT N'Pending',
        Comments             NVARCHAR(500) NULL,
        CreatedAt            DATETIME2(0) NOT NULL
            CONSTRAINT DF_TruckStop_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_TruckStop PRIMARY KEY CLUSTERED (TruckStopID),
        CONSTRAINT FK_TruckStop_Truck FOREIGN KEY (TruckID)
            REFERENCES dbo.Truck(TruckID),
        CONSTRAINT FK_TruckStop_Driver FOREIGN KEY (DriverID)
            REFERENCES dbo.Driver(DriverID),
        CONSTRAINT FK_TruckStop_WorkShift FOREIGN KEY (ShiftID)
            REFERENCES dbo.WorkShift(ShiftID),
        CONSTRAINT FK_TruckStop_Trip FOREIGN KEY (TripID)
            REFERENCES dbo.Trip(TripID),
        CONSTRAINT FK_TruckStop_StopReason FOREIGN KEY (StopReasonID)
            REFERENCES dbo.StopReason(StopReasonID),
        CONSTRAINT CK_TruckStop_EndTime
            CHECK (EndTime IS NULL OR EndTime >= StartTime),
        CONSTRAINT CK_TruckStop_ValidationStatus
            CHECK (ValidationStatus IN (N'Pending', N'Validated', N'Rejected'))
    );

    CREATE TABLE dbo.QualityResult (
        QualityResultID      BIGINT IDENTITY(1,1) NOT NULL,
        AnalysisDate         DATETIME2(0) NOT NULL,
        TripID               BIGINT NULL,
        LoadingPointID       INT NULL,
        QualityValue         DECIMAL(10,3) NOT NULL,
        QualityUnit          NVARCHAR(20) NOT NULL
            CONSTRAINT DF_QualityResult_QualityUnit DEFAULT N'P2O5Percent',
        TargetValue          DECIMAL(10,3) NULL,
        Status               NVARCHAR(20) NOT NULL
            CONSTRAINT DF_QualityResult_Status DEFAULT N'Pending',
        Comments             NVARCHAR(500) NULL,
        CreatedAt            DATETIME2(0) NOT NULL
            CONSTRAINT DF_QualityResult_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_QualityResult PRIMARY KEY CLUSTERED (QualityResultID),
        CONSTRAINT FK_QualityResult_Trip FOREIGN KEY (TripID)
            REFERENCES dbo.Trip(TripID),
        CONSTRAINT FK_QualityResult_LoadingPoint FOREIGN KEY (LoadingPointID)
            REFERENCES dbo.LoadingPoint(LoadingPointID),
        CONSTRAINT CK_QualityResult_Value
            CHECK (QualityValue >= 0),
        CONSTRAINT CK_QualityResult_TargetValue
            CHECK (TargetValue IS NULL OR TargetValue >= 0),
        CONSTRAINT CK_QualityResult_Status
            CHECK (Status IN (N'InTarget', N'BelowTarget', N'AboveTarget', N'Pending', N'Review')),
        CONSTRAINT CK_QualityResult_Source
            CHECK (TripID IS NOT NULL OR LoadingPointID IS NOT NULL)
    );

    CREATE INDEX IX_Trip_TripDate
        ON dbo.Trip(TripDate);

    CREATE INDEX IX_Trip_TruckID_TripDate
        ON dbo.Trip(TruckID, TripDate);

    CREATE INDEX IX_Trip_DriverID_TripDate
        ON dbo.Trip(DriverID, TripDate);

    CREATE INDEX IX_Trip_ShiftID
        ON dbo.Trip(ShiftID);

    CREATE INDEX IX_Trip_LoadingPointID
        ON dbo.Trip(LoadingPointID);

    CREATE INDEX IX_Trip_DestinationID
        ON dbo.Trip(DestinationID);

    CREATE INDEX IX_TruckStop_TruckID_StartTime
        ON dbo.TruckStop(TruckID, StartTime);

    CREATE INDEX IX_TruckStop_StopReasonID_StartTime
        ON dbo.TruckStop(StopReasonID, StartTime);

    CREATE INDEX IX_TruckStop_DriverID
        ON dbo.TruckStop(DriverID);

    CREATE INDEX IX_TruckStop_ShiftID
        ON dbo.TruckStop(ShiftID);

    CREATE INDEX IX_TruckStop_TripID
        ON dbo.TruckStop(TripID);

    CREATE INDEX IX_QualityResult_AnalysisDate
        ON dbo.QualityResult(AnalysisDate);

    CREATE INDEX IX_QualityResult_LoadingPointID_AnalysisDate
        ON dbo.QualityResult(LoadingPointID, AnalysisDate);

    CREATE INDEX IX_QualityResult_TripID
        ON dbo.QualityResult(TripID);

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
