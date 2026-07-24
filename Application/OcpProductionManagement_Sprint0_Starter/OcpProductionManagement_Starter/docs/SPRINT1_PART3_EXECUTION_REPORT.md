# Sprint 1 Part 3 execution report

## Scope and status mapping

Sprint 1 Part 3 implements the controlled prototype workflow:

```text
InProgress -> PendingValidation -> Completed
En cours      À valider            Validé
```

`Completed` remains the final reporting status. `PendingValidation` is a non-completed operational state used only after closure and before supervisor validation.

The controlled database compatibility change is documented in `docs/DB_CHANGE_001_PENDING_VALIDATION.md` and checkpointed in commit `47a59f8`. The database is frozen again; Part 3 makes no database-object change.

## Architecture

Application:

- `TripStatuses` centralizes the three workflow values and allowed transitions;
- `CloseTripCommand` contains only Trip ID, end time, and tonnage;
- `TripWorkflowResult` exposes safe success, missing-row, invalid-state, validation, concurrency, and persistence outcomes;
- `TripClosureData`, `PendingTripListItemDto`, and `TripValidationSearchCriteria` define safe read projections;
- `ITripWorkflowService` defines closure, pending queue/count, and validation operations.

Infrastructure:

- `TripWorkflowService` uses tracked generated `Trip` entities and `SaveChangesAsync`;
- closure changes only `EndTime`, `Tonnage`, and `Status`;
- validation changes only `Status`;
- pending queue/count queries use `AsNoTracking`;
- `TripWriteService` rechecks the current stored status before ordinary editing;
- the workflow service is registered through the existing Infrastructure DI extension.

Web:

- `TripWorkflowController` owns antiforgery-protected closure GET/POST routes under `/Trips/Close/{id}` and uses `CanManageTrips`;
- `ValidationController` owns `/Validation/Trips` and the antiforgery-protected validation POST, both using `CanValidate`;
- `TripCloseInputModel` cannot bind status, foreign keys, creation metadata, or generated entities;
- the pending queue is server-paged and supports search plus date filters;
- list/details actions are status-aware, and the supervisor dashboard/sidebar expose the existing validation center;
- `TripPresentation` centrally maps `InProgress`, `PendingValidation`, and `Completed` to French labels and restrained Bootstrap badges.

## Closure rules

Closure is accepted only when:

- the Trip exists;
- current status is exactly `InProgress`;
- end time is supplied and is not before start time;
- tonnage is supplied, strictly positive, and fits `decimal(10,2)`.

Equal start and end times remain accepted because the validated database rule is `EndTime >= StartTime`.

Successful closure changes only:

- `EndTime`;
- `Tonnage`;
- `Status` to `PendingValidation`.

## Validation rules

Validation is accepted only when:

- the Trip exists;
- current status is exactly `PendingValidation`;
- end time exists;
- tonnage exists and is positive.

Successful validation preserves all operational fields and changes only `Status` to `Completed`. Repeated validation and every other transition are rejected with a safe French message.

## Editing locks and authorization

- `InProgress` remains ordinarily editable by `CanManageTrips` users.
- `PendingValidation` and `Completed` are read-only for ordinary editing.
- list/details buttons hide Edit and Close outside `InProgress`.
- GET Edit redirects closed/validated voyages to read-only details.
- POST Edit reloads status before ModelState handling, and `TripWriteService` checks it again before assigning fields.
- `AgentProduction` and `Superviseur` retain `CanManageTrips`.
- only `Superviseur` retains `CanValidate`.
- no username-specific or administrator shortcut was introduced.
- every workflow POST uses `ValidateAntiForgeryToken` and accepts no browser-supplied status.

## Tests and rollback verification

The 81-test suite covers:

- technical values, French labels, neutral unknown-status display, and allowed/forbidden transitions;
- required closure fields, tonnage limits, and culture-safe comma/dot parsing;
- controller policies, antiforgery, route/form mismatch, safe redirects, queue behavior, and absence of forbidden endpoints;
- InProgress edit regression plus PendingValidation/Completed GET and forged-POST locks;
- real SQL Server create, invalid closure, equal-time closure, pending queue projection, validation, repeated-transition rejection, and missing row behavior.

The successful integration workflow runs inside an explicit EF Core SQL transaction and rolls back in `finally`. Before and after verification:

- Trip count: 801 / 801
- Trip fingerprint: `1490482480` / `1490482480`
- temporary Part 3 rows remaining: 0
- related `TruckStop` created: 0
- related `QualityResult` created: 0

## Quality, security, and runtime verification

- restore: passed;
- formatting and no-change verification: passed;
- Debug build: 0 warnings, 0 errors;
- tests: 81 passed, 0 failed, 0 skipped;
- vulnerable packages: none reported;
- generated entity/DbContext changes: none;
- migrations: none;
- raw SQL writes in application code: none;
- secret scan: no real connection string, credential, token, or production secret added.

Authenticated HTTP smoke testing at `http://localhost:5085` verified login, list/filter/pagination/details, state-aware closure visibility, closure form rendering, server rejection of an invalid end time, unchanged stored status, supervisor queue/empty state/dashboard count, role denials, antiforgery rejection, connected Diagnostics, logout, and anonymous redirects. Successful closure and validation were verified only inside the rollback integration transaction to avoid permanent data.

Automated visual-browser control was unavailable in the local tool runtime. Human review is recommended for desktop/mobile layout, visible focus, and the confirmation-button hierarchy. The application process was stopped and port 5085 was free afterward.

## Known limitations

The frozen schema has no fields for rejection, correction history, validator identity, validation date, or audit history. This part intentionally does not implement rejection, correction requests, reopening, cancellation, deletion, bulk validation, notifications, or automatic approval.

## Manual acceptance

1. As AgentProduction, open an `InProgress` voyage and confirm Modifier and Clôturer are visible.
2. Open the closure form and review the warning, final fields, and responsive layout.
3. In a safe test environment, close the voyage and confirm À valider plus the edit lock.
4. As Superviseur, review it in Centre de validation and validate it.
5. Confirm Validé and the persistent edit lock.
6. Confirm AgentProduction and AgentQualite cannot invoke supervisor validation.
