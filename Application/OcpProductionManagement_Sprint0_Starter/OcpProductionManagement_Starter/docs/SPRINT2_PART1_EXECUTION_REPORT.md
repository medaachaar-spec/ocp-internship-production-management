# Sprint 2 Part 1 execution report

## Scope

Sprint 2 Part 1 adds a strictly read-only Truck Stops module:

- `GET /Stops`;
- `GET /Stops/Details/{id}`;
- live SQL Server data;
- server-side search, filters, deterministic pagination, and empty states;
- French list and details screens;
- safe related Voyage navigation;
- role-aware sidebar integration through the existing `CanManageStops` policy.

No creation, editing, closure, validation, deletion, export, reporting, or Sprint 2 Part 2 workflow was added.

## Verified database mapping

The implementation was planned from live read-only metadata, actual view definitions, and generated Database-First code.

| UI field | SQL source | Generated model / DbSet | C# property | Availability |
|---|---|---|---|---|
| Stop ID | `dbo.TruckStop` / `dbo.vw_TruckStopDetails` | `TruckStop` / `TruckStops`; `vw_TruckStopDetail` / `vw_TruckStopDetails` | `TruckStopID` (`long`) | Yes |
| Voyage | nullable `TripID` | same sources | `TripID` (`long?`) | Yes, optional |
| Truck | `TruckID`, `TruckCode` | detail view or `Truck` navigation | `TruckID`, `TruckCode` | Yes |
| Driver | nullable `DriverID` and Driver reference | detail view or `Driver` navigation | `DriverID`, `DriverCode`, `DriverName` | Yes, optional |
| Work shift | nullable `ShiftID` and WorkShift reference | detail view or `Shift` navigation | `ShiftID`, `ShiftCode`, `ShiftName` | Yes, optional |
| Stop reason | required `StopReasonID` and StopReason reference | detail view or `StopReason` navigation | code, name, category, planned flag | Yes |
| Start | required `StartTime` | both sources | `StartTime` | Yes |
| End | nullable `EndTime` | both sources | `EndTime` | Yes, optional |
| Duration | persisted computed `DurationMinutes` | both sources | `DurationMinutes` | Yes, optional while open |
| Operational state | derived from `EndTime` | projected DTO | `State` | Yes, presentation-only |
| Validation state | required `ValidationStatus` | both sources | `ValidationStatus` | Yes |
| Observation | nullable `Comments` | both sources | `Comments` / DTO `Observation` | Yes, optional |
| Created at | required `CreatedAt` | `TruckStop` / `TruckStops` | `CreatedAt` | Yes on details |
| Route context | optional linked Trip references | `Trip` navigation | loading-point/destination code and name | Yes when linked |

`dbo.vw_TruckStopDetails` is keyless and supplies all list columns without N+1 queries. The details query uses `TruckStops.AsNoTracking()` and a single direct DTO projection so it can include `CreatedAt`, truck metadata, and safe linked-Trip context without editing a generated file. `vw_StopAnalysis` and `vw_StopsByReason` were inspected but are not needed for this operational read slice.

The live constraint `CK_TruckStop_ValidationStatus` permits exactly `Pending`, `Validated`, and `Rejected`. These are displayed as En attente, Validé, and Rejeté. Operational state is separately derived without persistence: null `EndTime` means Ouvert; non-null `EndTime` means Clôturé.

Unavailable schema fields were not invented: reporting author/source, modification timestamp, separate provisional/final reasons, estimated-time indicator, decision history, validator identity, and validation date do not exist in the frozen model.

## Documentation discrepancy

The functional PDFs describe read-only Stops access for ResponsableProduction and Administrateur. The actual validated application policy `CanManageStops` currently contains only AgentProduction and Superviseur. The task explicitly required reuse without broadening, so implementation follows the application policy and records the discrepancy here. No authorization policy or role mapping was changed.

## Architecture and behavior

Application contains safe DTOs, normalized criteria, filter options, centralized operational/validation states, and `IStopReadService`. The existing generic `PagedResult<T>` is reused.

Infrastructure contains `StopReadService`, registered by the existing DI extension. It uses only asynchronous EF Core read queries with `AsNoTracking`, cancellation tokens, SQL-side filters, `CountAsync`, deterministic `OrderByDescending` / `ThenByDescending`, `Skip`, `Take`, and direct projection. It does not call `SaveChanges` and contains no raw SQL.

Web contains a thin GET-only `StopsController`, dedicated view models/navigation/presentation helpers, a French responsive list, a French read-only detail page, and the enabled Arrêts sidebar link. The related Trip link is rendered only when a Trip exists and the current principal also satisfies `CanManageTrips`. There is no Truck link because no operational Truck page exists.

Search covers exact Stop/Trip identifiers, truck code, reason code/name, and observations. Filters cover inclusive date range, truck, stop reason, derived operational state, real validation status, and optional Trip ID. Page sizes are 10, 20, and 50. Filters and page size persist through pagination and detail return links.

## Tests and runtime verification

The 108-test suite covers:

- criteria normalization and safe zero-result pagination;
- supported page sizes, valid/invalid state and status values, and date retention;
- French state/status labels, duration formatting, null end time, unknown status, and null observation fallbacks;
- controller policy, GET-only surface, normalized Index behavior, details/404 behavior, route preservation, and Trip-link authorization;
- DI registration;
- live SQL Server list/details projection, search, all filters, pagination, options, missing ID, deterministic ordering, and unchanged fingerprint;
- all prior Sprint 0 and Sprint 1 regressions.

Final quality results:

- restore: passed;
- formatting and no-change verification: passed;
- Debug build: 0 warnings, 0 errors;
- tests: 108 passed, 0 failed, 0 skipped;
- vulnerable packages: none reported, including transitive packages.

Installed headless Edge executed 34 authenticated/anonymous checks at `http://localhost:5085`:

- AgentProduction login, visible/active Arrêts navigation, live 150-row total, search, date/truck/reason/state/status combined filters, persistence, reset, empty state, page size, pagination, valid detail, invalid 404, related Voyage link, and absence of write actions;
- anonymous redirect to login;
- AgentQualite Access Denied and hidden Arrêts navigation;
- Voyage list regression, connected Diagnostics, and logout;
- desktop and approximately 768 px page-level overflow checks, visible labels, screenshots reviewed, and zero console/page errors.

The in-app browser runtime could not initialize its local assets, so installed Edge and authenticated HTTP/browser automation were used. Temporary browser and PDF-review artifacts were removed afterward.

## Database and repository safety

Before and after the implementation and runtime tests:

- `TruckStop` count: 150 / 150;
- `TruckStop` fingerprint: `-1898382792` / `-1898382792`.

No data write, `SaveChanges`, migration, schema operation, stored procedure, trigger, raw SQL write, Database-First regeneration, generated entity edit, or generated DbContext edit occurred. The real connection remains only in .NET User Secrets; repository appsettings contains an empty value and the report contains no secret.

The repository-wide schema-write scan finds only the previously approved, versioned, untouched Trip constraint compatibility script. No task-owned application or browser process remains and port 5085 is free.

## Known limitations

- All 150 current prototype stops are closed and validated, so Open, Pending, and Rejected filters intentionally demonstrate the empty state until such frozen data exists.
- The current policy differs from broader read-only roles described in the functional PDFs; it was deliberately preserved.
- Truck details navigation is withheld because that module does not yet exist.
- The database has no separate provisional/final reason, reporting author/source, modification timestamp, or decision-history fields.

## Next recommended task

Sprint 2 Part 2 - implement Truck Stop creation and editing after explicit authorization.
