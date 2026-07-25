USE [OCP_Mining_Prototype];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Trip', N'U') IS NULL
    BEGIN
        THROW 50001, 'The table dbo.Trip does not exist.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Trip')
          AND name = N'CK_Trip_Status'
    )
    BEGIN
        THROW 50002, 'The constraint CK_Trip_Status does not exist.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Trip
        WHERE [Status] NOT IN
        (
            N'Incomplete',
            N'Cancelled',
            N'Completed',
            N'InProgress',
            N'Planned'
        )
    )
    BEGIN
        THROW 50003, 'Unexpected existing Trip.Status value detected.', 1;
    END;

    ALTER TABLE dbo.Trip
        DROP CONSTRAINT CK_Trip_Status;

    ALTER TABLE dbo.Trip WITH CHECK
        ADD CONSTRAINT CK_Trip_Status
        CHECK
        (
            [Status] IN
            (
                N'Incomplete',
                N'Cancelled',
                N'Completed',
                N'InProgress',
                N'Planned',
                N'PendingValidation'
            )
        );

    ALTER TABLE dbo.Trip
        CHECK CONSTRAINT CK_Trip_Status;

    COMMIT TRANSACTION;

    PRINT 'CK_Trip_Status successfully updated.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO