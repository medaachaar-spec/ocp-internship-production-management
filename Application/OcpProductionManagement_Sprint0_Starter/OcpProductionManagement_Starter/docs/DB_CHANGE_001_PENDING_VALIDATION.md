# DB Change 001 - Pending validation status

## Scope

- Database: `OCP_Mining_Prototype`
- Object: `dbo.Trip`
- Constraint: `CK_Trip_Status`
- Versioned script: `scripts/06_add_pending_validation_status.sql`

This controlled change adds the technical value `PendingValidation` required by the validated voyage workflow. It represents a closed voyage awaiting supervisor validation. `Completed` remains the final validated and reporting status; `PendingValidation` remains a non-completed state in the existing reporting views.

## Allowed values

Previously allowed:

- `Incomplete`
- `Cancelled`
- `Completed`
- `InProgress`
- `Planned`

Allowed after the change:

- `Incomplete`
- `Cancelled`
- `Completed`
- `InProgress`
- `Planned`
- `PendingValidation`

The live `CK_Trip_Status` constraint was verified as enabled (`is_disabled = 0`) and trusted (`is_not_trusted = 0`). Its definition contains exactly the six values above.

## Structural safety

Only `CK_Trip_Status` was replaced. No table, column, key, relationship, index, SQL view, generated EF model, or data row was structurally changed. No EF migration was created. The database schema is frozen again after this controlled compatibility change.

The versioned script:

- uses `SET XACT_ABORT ON` and an explicit transaction;
- verifies that `dbo.Trip` and `CK_Trip_Status` exist;
- rejects unexpected pre-change status values;
- changes only `CK_Trip_Status`;
- recreates the constraint using `WITH CHECK` and enables it;
- rolls back and rethrows on error.

## Verification evidence

Live metadata verification confirmed:

- `dbo.Trip.Status` is `nvarchar(20) NOT NULL`;
- current status distribution is `Completed` 782, `Incomplete` 13, and `InProgress` 6;
- `PendingValidation` is accepted by the constraint;
- `Completed` continues to drive completed-trip reporting in `vw_TripSummary`, `vw_TripDetails`, daily/global summaries, and truck/driver performance views.

Rollback compatibility test:

1. Trip `156`, originally `InProgress`, was selected inside a SQL transaction.
2. Only its `Status` was updated to `PendingValidation`.
3. The new value was reloaded successfully inside the transaction.
4. The transaction was rolled back.
5. The row reloaded as `InProgress` afterward.

Before and after rollback:

- Trip count: 801 / 801
- Trip fingerprint: `1490482480` / `1490482480`
- permanent `PendingValidation` test rows: 0
- `TruckStop` fingerprint: unchanged
- `QualityResult` fingerprint: unchanged

No backup artifact or backup-operation record is present in the repository, so backup completion was not independently verified by this application task. The database change itself was manually applied before this verification and was tested without leaving a data modification.
