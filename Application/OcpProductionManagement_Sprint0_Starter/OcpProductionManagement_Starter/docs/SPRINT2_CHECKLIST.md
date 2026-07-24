# Sprint 2 checklist

## Part 1 - read-only Truck Stops

- [x] Clean Sprint 1 Part 3 baseline `3a85b92` confirmed before implementation
- [x] Workspace-level functional, navigation, screen-inventory, UI/UX, and database documentation reviewed
- [x] Live frozen SQL metadata, constraints, foreign keys, view definitions, values, count, and fingerprint verified read-only
- [x] Generated `TruckStop`, `vw_TruckStopDetail`, `vw_StopAnalysis`, `vw_StopsByReason`, and DbSet names inspected
- [x] `BIGINT` / `long` TruckStop primary-key mapping confirmed
- [x] `vw_TruckStopDetails` selected for the list and filter options
- [x] Direct no-tracking `TruckStop` projection selected for details and operational context
- [x] Real `ValidationStatus` values centralized: `Pending`, `Validated`, and `Rejected`
- [x] Operational state derived read-only from `EndTime`: `Open` / `Closed`
- [x] Application DTOs, search criteria, filter options, state/status constants, and read-service contract added
- [x] Infrastructure read service uses `AsNoTracking`, async EF Core, cancellation tokens, direct DTO projection, and no raw SQL
- [x] Free-text search executes server-side for verified identifiers and text fields
- [x] Period, truck, reason, state, validation-status, and optional Trip filters execute server-side
- [x] End-date filter includes the complete selected calendar date through a next-day exclusive boundary
- [x] Deterministic ordering, `CountAsync`, `Skip`, and `Take` execute server-side
- [x] Page sizes limited to 10, 20, and 50; invalid values normalize safely
- [x] French responsive list, explicit empty state, accessible labels, and internal table scrolling implemented
- [x] Read-only detail sections and authorized related Voyage link implemented
- [x] No incomplete Truck route or future write action exposed
- [x] `StopsController` exposes GET Index and Details only and uses `CanManageStops`
- [x] Sidebar Arrêts entry enabled only for the existing AgentProduction/Superviseur visibility branch
- [x] Existing policy matrix remains unchanged
- [x] Anonymous access redirects to login
- [x] AgentQualite access follows normal Access Denied behavior
- [x] Focused criteria, presentation, controller, DI, and live read-only integration tests added
- [x] Restore, formatting, formatting verification, and Debug build pass
- [x] Build result: 0 warnings, 0 errors
- [x] Test result: 108 passed, 0 failed, 0 skipped
- [x] NuGet reports no vulnerable direct or transitive packages
- [x] Runtime suite passes: 34 checks, 0 failed, 0 console/page errors
- [x] Desktop and approximately 768 px layouts verified without page-level overflow
- [x] TruckStop count remains 150 and fingerprint remains `-1898382792`
- [x] No data write, SaveChanges, migration, schema change, raw SQL write, model regeneration, or secret exposure occurred
- [x] No task-owned application process remains and port 5085 is free
- [x] Sprint 2 Part 2 was not started

## Part 2 - Truck Stop creation and editing

- [x] Clean Part 1 baseline `be99b3d` confirmed before implementation
- [x] Live `dbo.TruckStop` columns, defaults, computed duration, foreign keys, indexes, and trusted constraints reverified
- [x] Editable mapping limited to optional `TripID`, required `TruckID`, optional `DriverID`/`ShiftID`, required `StopReasonID`, `StartTime`, and `Comments`
- [x] Identity `TruckStopID`, SQL-default `CreatedAt`, computed `DurationMinutes`, `EndTime`, and workflow status protected from HTTP binding
- [x] Dedicated Create/Edit input models, application commands/results/data/options, and `IStopWriteService` added
- [x] EF Core write service performs explicit field mapping and writes only `dbo.TruckStop`
- [x] New stops are open (`EndTime = null`) and Pending
- [x] Active/selectable Truck, Driver, WorkShift, and StopReason checks implemented
- [x] Optional Trip existence plus Truck/Driver/Shift consistency checks implemented
- [x] Edit allowed only while `EndTime` is null and `ValidationStatus` is Pending
- [x] POST Edit reloads state and rejects forged route/form identifiers and locked rows
- [x] `CanManageStops` policy preserved; both POST actions use antiforgery
- [x] No Close, Validate, Reject, Reopen, Delete, or Cancel endpoint introduced
- [x] French responsive Create/Edit forms, list action, details action, validation, and Post/Redirect/Get implemented
- [x] Rollback integration test creates, edits, locks, and removes a unique temporary stop through transaction rollback
- [x] TruckStop before/after: 150 rows; fingerprint `-1898382792`
- [x] Trip, Truck, Driver, WorkShift, StopReason, and QualityResult counts/fingerprints unchanged by rollback tests
- [x] Runtime suite: 30 passed, 0 failed; invalid submissions created no row
- [x] Desktop and 768 px layouts have no page-level horizontal overflow; no console/page errors observed
- [x] Diagnostics connected, Voyages regression page loaded, logout worked, anonymous redirects and AgentQualite denial verified
- [x] Formatting verification, Debug build, full tests, package vulnerability scan, and secret/security scan completed
- [x] No migration, schema change, generated-model edit, raw SQL write, or secret exposure
- [x] Sprint 2 Part 3 was not started

## Part 3 - Truck Stop closure and supervisor validation

- [x] Clean Part 2 Git baseline `f11ea53` confirmed before implementation
- [x] Workspace functional, UI/UX, navigation, database, and technical architecture documentation reviewed
- [x] Live `dbo.TruckStop` mapping, computed duration, defaults, and trusted constraints reverified read-only
- [x] Before baseline recorded: 151 stops; fingerprint `-1070686150`; 150 Closed/Validated and 1 Open/Pending
- [x] Centralized rules enforce only Open/Pending to Closed/Pending to Closed/Validated
- [x] Closure assigns only `EndTime`; `ValidationStatus` remains Pending
- [x] Validation assigns only `ValidationStatus = Validated`; operational fields remain unchanged
- [x] Closure GET/POST uses `/Stops/Close/{id}`, `CanManageStops`, antiforgery, and route/form ID matching
- [x] Closure input exposes only Stop ID and required EndTime; status is never accepted from HTTP input
- [x] EndTime before StartTime is rejected in the workflow service and by the trusted database constraint
- [x] Supervisor queue uses `/Validation/Stops`, `CanValidate`, server-side search/filtering, page sizes 10/20/50, and oldest-closure ordering
- [x] Validation POST uses `/Validation/Stops/{id}/Validate`, `CanValidate`, and antiforgery
- [x] Closed/Pending and Validated stops are locked in both controller and service edit boundaries
- [x] Forged closure, validation, repeat-transition, and edit attempts are rejected server-side
- [x] Validation Center exposes separate Voyages and Arrêts queues; supervisor dashboard shows both counts
- [x] No rejection, correction, reopening, deletion, cancellation, history, validator identity, or validation date implemented
- [x] Rollback SQL integration test exercises closure, queue ordering, validation, repeated-transition rejection, and edit locks
- [x] Post-test/post-runtime baseline unchanged: 151 stops; fingerprint `-1070686150`; no test marker remains
- [x] Trip and QualityResult counts/fingerprints unchanged: 801 / `-1565377314`; 120 / `310868675`
- [x] Restore, formatting, formatting verification, Debug build, and full tests pass
- [x] Build result: 0 warnings, 0 errors
- [x] Test result: 145 passed, 0 failed, 0 skipped
- [x] NuGet reports no vulnerable direct or transitive packages
- [x] Browser/HTTP verification covers 44 workflow, authorization, antiforgery, diagnostics, navigation, and responsive-layout checks
- [x] No browser console/page error observed; Diagnostics displays `Connectée`
- [x] No migration, schema change, generated-model edit, raw SQL write, secret exposure, or persistent test write
- [x] No task-owned application process remains and port 5085 is free
- [x] Sprint 3 was not started
