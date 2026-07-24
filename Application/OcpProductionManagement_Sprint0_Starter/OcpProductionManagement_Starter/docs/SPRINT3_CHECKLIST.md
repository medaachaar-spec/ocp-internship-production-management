# Sprint 3 checklist

## Phase A - compatibility and safety

- [x] Clean Sprint 2 Part 3 baseline `ba53c99` confirmed
- [x] Functional, screen, navigation, UI/UX, architecture, and database source documents reviewed
- [x] Relevant PDF pages rendered and visually inspected
- [x] `dbo.QualityResult`, `dbo.Trip`, and `dbo.vw_QualityAnalysis` inspected with read-only metadata queries
- [x] Generated entities, DbSets, keyless view mapping, precision, defaults, constraints, indexes, and foreign keys verified
- [x] `BIGINT` / `long` identity mapping confirmed; existing IDs can be zero
- [x] Existing status distribution and 120-row baseline recorded
- [x] Read-only and creation/editing classified compatible
- [x] Validation workflow classified not applicable because no final lifecycle value or transition exists

## Phase B - read-only Quality

- [x] Application DTOs, criteria, filter options, actual status definitions, and read-service contract added
- [x] `QualityReadService` uses `AsNoTracking`, async EF Core, cancellation tokens, direct projection, and no raw SQL
- [x] Search, date, Voyage, truck, P₂O₅ range, and actual-status filters execute in SQL Server
- [x] `CountAsync`, deterministic ordering, `Skip`, and `Take` execute server-side
- [x] Page sizes limited to 10, 20, and 50; invalid criteria normalize safely
- [x] French list and details views include accessible filters, responsive table scrolling, pagination, and explicit empty state
- [x] Related Voyage link appears only when `CanManageTrips` succeeds
- [x] Quality sidebar entry enabled for the existing AgentQualite/Superviseur visibility branch
- [x] `QualityController` protected by `CanManageQuality`
- [x] Criteria, presentation, controller, and live read-only fingerprint tests added
- [x] Phase B build: 0 warnings, 0 errors
- [x] Phase B tests: 166 passed, 0 failed, 0 skipped
- [x] Read-only checkpoint `c852ff8` created with message `Implement Sprint 3 read-only Quality module`

## Phase C - creation and correction

- [x] Dedicated Create/Edit input models expose no EF entity or protected field
- [x] Writable mapping limited to analysis date, optional Voyage, optional loading point, P₂O₅ value, and observation
- [x] Voyage or loading point required; both sources are checked for consistency
- [x] `decimal(10,3)`, non-negative, and no-changed-fourth-decimal validation enforced in Web and service boundaries
- [x] French decimal comma and browser decimal point parsing supported
- [x] Identity, `QualityUnit`, `TargetValue`, `Status`, and `CreatedAt` protected
- [x] Create uses `AddAsync`; Edit loads the tracked row and assigns only authorized fields
- [x] `DbSet.Update`, raw SQL writes, related-table writes, deletion, and validation transitions are absent
- [x] GET and POST Create/Edit implemented under `CanManageQuality`
- [x] Both POST actions use antiforgery, ModelState, safe service errors, and Post/Redirect/Get
- [x] POST Edit verifies route/form identifiers and reloads current state independently
- [x] Correction remains available because current statuses are analytical, not lifecycle locks
- [x] Rollback integration test creates, reloads defaults, updates, checks protected fields, rejects invalid references/relationships/precision, and rolls back in `finally`
- [x] Temporary marker and row are absent after rollback
- [x] QualityResult, Trip, Truck, and LoadingPoint before/after snapshots match
- [x] Phase C build: 0 warnings, 0 errors
- [x] Phase C tests: 191 passed, 0 failed, 0 skipped
- [x] Creation/editing checkpoint `01f8e65` created with message `Implement Sprint 3 Quality creation and editing`

## Phase D - validation workflow

- [x] `Quality validation workflow: NOT APPLICABLE`
- [x] No Quality validation queue, action, button, `CanValidate` endpoint, transition, or edit lock invented
- [x] No Phase D commit created

## Phase E - complete verification

- [x] Restore passed
- [x] Formatting and formatting verification passed
- [x] Debug build passed with 0 warnings and 0 errors
- [x] Full test suite passed: 191 passed, 0 failed, 0 skipped
- [x] NuGet reported no vulnerable direct or transitive packages
- [x] Security review confirmed policy, antiforgery, route/form ID checks, protected input boundary, state reload, and safe errors
- [x] Repository scan found no real connection string, password, API key, token, or credential
- [x] Placeholder connection syntax remains documentation-only; the real value remains in User Secrets
- [x] No migration, schema change, generated-model edit, raw SQL write, or invented conformity threshold exists
- [x] Runtime suite passed: 49 checks, 0 failed, 0 browser console errors, 0 page errors
- [x] AgentQualite and Superviseur Quality access verified; AgentProduction denial verified
- [x] List, combined filters, pagination, empty state, details, Create/Edit boundaries, antiforgery denial, and Diagnostics verified
- [x] Voyages, Stops, and `/Validation/Trips` regressions verified
- [x] Desktop and approximately 768px layouts visually reviewed with no page-level overflow
- [x] QualityResult remains 120 rows with fingerprint `310868675`; rollback marker count is 0
- [x] Related-table final fingerprints recorded in the execution report
- [x] No task-owned app process remains and port 5085 is free
- [x] Sprint 4 has not started

## Final manual visual acceptance

- [ ] As AgentQualite, review the Quality list and one detail page with the project owner’s preferred browser and display scaling.
- [ ] Create or correct a legitimate operational measurement only after the business owner authorizes a permanent data write.
- [ ] Confirm with the data owner whether a future Quality validation lifecycle will require a controlled database change.
