/*
    Script 02 - Seed reference data

    Creates deterministic fictional reference data for the prototype.
    Run after sql/01_create_database.sql.
*/

USE OCP_Mining_Prototype;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    MERGE dbo.Truck WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'TRK-001', N'PROTO-001', N'Tombereau classe 55 t', 55.00, N'InService', 1),
        (N'TRK-002', N'PROTO-002', N'Tombereau classe 55 t', 55.00, N'InService', 1),
        (N'TRK-003', N'PROTO-003', N'Tombereau classe 55 t', 55.00, N'InService', 1),
        (N'TRK-004', N'PROTO-004', N'Tombereau classe 70 t', 70.00, N'InService', 1),
        (N'TRK-005', N'PROTO-005', N'Tombereau classe 70 t', 70.00, N'InService', 1),
        (N'TRK-006', N'PROTO-006', N'Tombereau classe 70 t', 70.00, N'InService', 1),
        (N'TRK-007', N'PROTO-007', N'Tombereau classe 70 t', 70.00, N'InService', 1),
        (N'TRK-008', N'PROTO-008', N'Tombereau classe 90 t', 90.00, N'InService', 1),
        (N'TRK-009', N'PROTO-009', N'Tombereau classe 90 t', 90.00, N'InService', 1),
        (N'TRK-010', N'PROTO-010', N'Tombereau classe 90 t', 90.00, N'InService', 1),
        (N'TRK-011', N'PROTO-011', N'Tombereau classe 90 t', 90.00, N'Available', 1),
        (N'TRK-012', N'PROTO-012', N'Tombereau classe 90 t', 90.00, N'Available', 1)
    ) AS source (TruckCode, RegistrationNumber, Model, RatedPayloadTonnes, Status, IsActive)
        ON target.TruckCode = source.TruckCode
    WHEN MATCHED THEN
        UPDATE SET
            RegistrationNumber = source.RegistrationNumber,
            Model = source.Model,
            RatedPayloadTonnes = source.RatedPayloadTonnes,
            Status = source.Status,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (TruckCode, RegistrationNumber, Model, RatedPayloadTonnes, Status, IsActive)
        VALUES (source.TruckCode, source.RegistrationNumber, source.Model, source.RatedPayloadTonnes, source.Status, source.IsActive);

    MERGE dbo.Driver WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'DRV-001', N'Conducteur fictif 01', N'PROTO-LIC-001', N'Active', 1),
        (N'DRV-002', N'Conducteur fictif 02', N'PROTO-LIC-002', N'Active', 1),
        (N'DRV-003', N'Conducteur fictif 03', N'PROTO-LIC-003', N'Active', 1),
        (N'DRV-004', N'Conducteur fictif 04', N'PROTO-LIC-004', N'Active', 1),
        (N'DRV-005', N'Conducteur fictif 05', N'PROTO-LIC-005', N'Active', 1),
        (N'DRV-006', N'Conducteur fictif 06', N'PROTO-LIC-006', N'Active', 1),
        (N'DRV-007', N'Conducteur fictif 07', N'PROTO-LIC-007', N'Active', 1),
        (N'DRV-008', N'Conducteur fictif 08', N'PROTO-LIC-008', N'Active', 1),
        (N'DRV-009', N'Conducteur fictif 09', N'PROTO-LIC-009', N'Active', 1),
        (N'DRV-010', N'Conducteur fictif 10', N'PROTO-LIC-010', N'Active', 1),
        (N'DRV-011', N'Conducteur fictif 11', N'PROTO-LIC-011', N'Active', 1),
        (N'DRV-012', N'Conducteur fictif 12', N'PROTO-LIC-012', N'Active', 1),
        (N'DRV-013', N'Conducteur fictif 13', N'PROTO-LIC-013', N'Active', 1),
        (N'DRV-014', N'Conducteur fictif 14', N'PROTO-LIC-014', N'Active', 1),
        (N'DRV-015', N'Conducteur fictif 15', N'PROTO-LIC-015', N'Active', 1),
        (N'DRV-016', N'Conducteur fictif 16', N'PROTO-LIC-016', N'Active', 1),
        (N'DRV-017', N'Conducteur fictif 17', N'PROTO-LIC-017', N'Active', 1),
        (N'DRV-018', N'Conducteur fictif 18', N'PROTO-LIC-018', N'Active', 1),
        (N'DRV-019', N'Conducteur fictif 19', N'PROTO-LIC-019', N'Active', 1),
        (N'DRV-020', N'Conducteur fictif 20', N'PROTO-LIC-020', N'Active', 1)
    ) AS source (DriverCode, FullName, LicenseNumber, Status, IsActive)
        ON target.DriverCode = source.DriverCode
    WHEN MATCHED THEN
        UPDATE SET
            FullName = source.FullName,
            LicenseNumber = source.LicenseNumber,
            Status = source.Status,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (DriverCode, FullName, LicenseNumber, Status, IsActive)
        VALUES (source.DriverCode, source.FullName, source.LicenseNumber, source.Status, source.IsActive);

    MERGE dbo.WorkShift WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'SHIFT-A', N'Poste A', CAST('06:00:00' AS TIME(0)), CAST('14:00:00' AS TIME(0)), 1),
        (N'SHIFT-B', N'Poste B', CAST('14:00:00' AS TIME(0)), CAST('22:00:00' AS TIME(0)), 1),
        (N'SHIFT-C', N'Poste C', CAST('22:00:00' AS TIME(0)), CAST('06:00:00' AS TIME(0)), 1)
    ) AS source (ShiftCode, ShiftName, StartTime, EndTime, IsActive)
        ON target.ShiftCode = source.ShiftCode
    WHEN MATCHED THEN
        UPDATE SET
            ShiftName = source.ShiftName,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ShiftCode, ShiftName, StartTime, EndTime, IsActive)
        VALUES (source.ShiftCode, source.ShiftName, source.StartTime, source.EndTime, source.IsActive);

    MERGE dbo.LoadingPoint WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'LP-01', N'Point de chargement 01', N'Mine de Boucraa', N'Code interne fictif du prototype - nom réel à confirmer avec l''encadrant.', 1),
        (N'LP-02', N'Point de chargement 02', N'Mine de Boucraa', N'Code interne fictif du prototype - nom réel à confirmer avec l''encadrant.', 1),
        (N'LP-03', N'Point de chargement 03', N'Mine de Boucraa', N'Code interne fictif du prototype - nom réel à confirmer avec l''encadrant.', 1),
        (N'LP-04', N'Point de chargement 04', N'Mine de Boucraa', N'Code interne fictif du prototype - nom réel à confirmer avec l''encadrant.', 1)
    ) AS source (LoadingPointCode, LoadingPointName, ZoneName, LoadingPointDescription, IsActive)
        ON target.LoadingPointCode = source.LoadingPointCode
    WHEN MATCHED THEN
        UPDATE SET
            LoadingPointName = source.LoadingPointName,
            ZoneName = source.ZoneName,
            LoadingPointDescription = source.LoadingPointDescription,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (LoadingPointCode, LoadingPointName, ZoneName, LoadingPointDescription, IsActive)
        VALUES (source.LoadingPointCode, source.LoadingPointName, source.ZoneName, source.LoadingPointDescription, source.IsActive);

    MERGE dbo.Destination WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'DEST-PROCESS', N'Alimentation traitement', N'Plant', 1),
        (N'DEST-STOCK', N'Stockage intermédiaire', N'Stockpile', 1),
        (N'DEST-DUMP', N'Point de déchargement interne', N'Other', 1)
    ) AS source (DestinationCode, DestinationName, DestinationType, IsActive)
        ON target.DestinationCode = source.DestinationCode
    WHEN MATCHED THEN
        UPDATE SET
            DestinationName = source.DestinationName,
            DestinationType = source.DestinationType,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (DestinationCode, DestinationName, DestinationType, IsActive)
        VALUES (source.DestinationCode, source.DestinationName, source.DestinationType, source.IsActive);

    MERGE dbo.StopReason WITH (HOLDLOCK) AS target
    USING (VALUES
        (N'TIRE_FAILURE', N'Panne pneu', N'Mechanical', 0, 1),
        (N'BRAKE_FAILURE', N'Panne freinage', N'Mechanical', 0, 1),
        (N'HYDRAULIC_FAILURE', N'Panne hydraulique', N'Mechanical', 0, 1),
        (N'ELECTRICAL_FAILURE', N'Panne électrique', N'Mechanical', 0, 1),
        (N'PREVENTIVE_MAINTENANCE', N'Maintenance préventive', N'Maintenance', 1, 1),
        (N'WAIT_LOADING', N'Attente au chargement', N'Operational', 0, 1),
        (N'WAIT_UNLOADING', N'Attente au déchargement', N'Operational', 0, 1),
        (N'ROAD_OR_SAFETY_DELAY', N'Blocage route / arrêt sécurité', N'Operational', 0, 1)
    ) AS source (ReasonCode, ReasonName, Category, IsPlanned, IsActive)
        ON target.ReasonCode = source.ReasonCode
    WHEN MATCHED THEN
        UPDATE SET
            ReasonName = source.ReasonName,
            Category = source.Category,
            IsPlanned = source.IsPlanned,
            IsActive = source.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ReasonCode, ReasonName, Category, IsPlanned, IsActive)
        VALUES (source.ReasonCode, source.ReasonName, source.Category, source.IsPlanned, source.IsActive);

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
