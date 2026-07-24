# Sprint 3 Quality compatibility matrix

## Decision summary

| Capability | Database support | Documentation support | Decision |
|---|---:|---:|---|
| Read list | Yes | Yes | Implemented from `dbo.vw_QualityAnalysis` |
| Read details | Yes | Yes | Implemented by joining the view to `dbo.QualityResult` |
| Search | Yes | Yes | Result ID and verified operational text fields |
| Date filters | Yes | Yes | `AnalysisDate`, inclusive calendar boundaries |
| Trip filter | Yes | Yes | Numeric `TripID`, including verified ID `0` |
| Truck filter | Yes | Yes | `TruckID` exposed by the analysis view |
| P₂O₅ range filter | Yes | Yes | `P2O5Percent`, filtered server-side without interpretation |
| Create | Yes | Yes | Implemented with protected SQL defaults |
| Edit | Yes | Yes | Implemented for correction of recorded measurements |
| Status field | Yes | Partial | Display the actual analytical value only |
| Validation workflow | No | Conceptual only | `Quality validation workflow: NOT APPLICABLE` |
| Concurrency token | No | N/A | Tracked-row update plus safe delete/update exception handling; no strong version check |

The maximum safe Sprint 3 scope is list, filters, details, creation, and correction. A validation lifecycle is excluded because no verified final validation value or supported transition exists in the frozen database.

## Verified `dbo.QualityResult` mapping

- Primary key: `QualityResultID BIGINT IDENTITY`, generated C# type `long`.
- Existing identities include `0`; Quality controller and filters therefore accept non-negative identifiers.
- No rowversion, concurrency token, `UpdatedAt`, or computed column exists.

| SQL column | SQL definition | Generated property | Behavior |
|---|---|---|---|
| `QualityResultID` | `bigint`, not null, identity, primary key | `QualityResultID` / `long` | Generated, protected |
| `AnalysisDate` | `datetime2(0)`, not null | `AnalysisDate` / `DateTime` | Writable |
| `TripID` | `bigint`, null | `TripID` / `long?` | Writable optional FK |
| `LoadingPointID` | `int`, null | `LoadingPointID` / `int?` | Writable optional FK |
| `QualityValue` | `decimal(10,3)`, not null | `QualityValue` / `decimal` | Writable, `>= 0` |
| `QualityUnit` | `nvarchar(20)`, not null, default `P2O5Percent` | `QualityUnit` / `string` | Database-managed on Create; protected on Edit |
| `TargetValue` | `decimal(10,3)`, null | `TargetValue` / `decimal?` | Protected; no target is invented |
| `Status` | `nvarchar(20)`, not null, default `Pending` | `Status` / `string` | Database-managed on Create; protected on Edit |
| `Comments` | `nvarchar(500)`, null | `Comments` / `string?` | Writable |
| `CreatedAt` | `datetime2(0)`, not null, default `sysutcdatetime()` | `CreatedAt` / `DateTime` | Generated, protected |

Verified relationships:

- `FK_QualityResult_Trip`: optional `TripID` to `dbo.Trip.TripID`, `NO ACTION`;
- `FK_QualityResult_LoadingPoint`: optional `LoadingPointID` to `dbo.LoadingPoint.LoadingPointID`, `NO ACTION`;
- `CK_QualityResult_Source`: at least one of `TripID` or `LoadingPointID` must be non-null;
- when both are supplied, the application verifies that the loading point matches the selected Voyage.

Verified checks are enabled and trusted:

- `QualityValue >= 0`;
- `TargetValue IS NULL OR TargetValue >= 0`;
- `Status IN ('Review', 'Pending', 'AboveTarget', 'BelowTarget', 'InTarget')`;
- at least one source relationship is present.

Current status distribution:

| Status | Rows |
|---|---:|
| `AboveTarget` | 18 |
| `BelowTarget` | 19 |
| `InTarget` | 80 |
| `Review` | 3 |
| `Pending` | 0 |

These values are stored analytical classifications. They are displayed neutrally and are not converted into a conformity rule.

## Verified `dbo.vw_QualityAnalysis` mapping

The generated DbSet is `vw_QualityAnalyses`. EF Core configures it with `HasNoKey().ToView("vw_QualityAnalysis")`.

Verified columns are: `QualityResultID`, `TripID`, `LoadingPointID`, `AnalysisDate`, `QualityDate`, `QualityUnit`, `P2O5Percent`, `TargetValue`, `QualityStatus`, `HasLinkedTrip`, `TripDate`, `TripStatus`, `TruckID`, `TruckCode`, `DriverID`, `DriverCode`, `DriverFullName`, `ShiftID`, `ShiftCode`, `ShiftName`, `LoadingPointCode`, `LoadingPointName`, `DestinationID`, `DestinationCode`, `DestinationName`, `IsP2O5Measurement`, `HasValidP2O5`, `LatestResultForTrip`, `AnalysisSequenceForTrip`, and `ResultCountForTrip`.

The list uses this view for operational context. Details join the view to the base row so comments and creation metadata remain available without exposing EF entities to Web.

## Writable field map

| Form field | Entity property | SQL column | Type | Required | Editable | Rule |
|---|---|---|---|---:|---:|---|
| Analysis date | `AnalysisDate` | `AnalysisDate` | `datetime2(0)` | Yes | Yes | Non-default value |
| Voyage | `TripID` | `TripID` | `bigint` | Conditional | Yes | Existing non-negative ID; must match loading point when both exist |
| Loading point | `LoadingPointID` | `LoadingPointID` | `int` | Conditional | Yes | Existing ID; Voyage or loading point required |
| P₂O₅ value | `QualityValue` | `QualityValue` | `decimal(10,3)` | Yes | Yes | `0` through `9,999,999.999`; no changed fourth decimal |
| Observation | `Comments` | `Comments` | `nvarchar(500)` | No | Yes | Trimmed; 500 characters maximum |

`QualityResultID`, `QualityUnit`, `TargetValue`, `Status`, and `CreatedAt` are absent from HTTP input models. No conformity field or threshold field exists.

## Validation incompatibility

Validated documents describe a future conceptual validation step, but the database does not supply a compatible lifecycle. In particular:

- there is no verified `Validated` or equivalent final status;
- `AboveTarget`, `BelowTarget`, and `InTarget` describe stored comparison bands;
- `Review` and `Pending` do not have a constraint-supported transition to a final validation value;
- no validator, validation date, or validation history field exists.

Therefore no `CanValidate` endpoint, queue, button, transition, audit field, or edit lock was added for Quality.
