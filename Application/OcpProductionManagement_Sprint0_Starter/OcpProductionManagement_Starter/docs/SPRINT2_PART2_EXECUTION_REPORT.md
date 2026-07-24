# Sprint 2 Part 2 execution report

## Scope and outcome

Sprint 2 Part 2 adds only Truck Stop creation and protected editing. It preserves the Part 1 list/details module and the complete Voyages module. Closure, supervisor validation, rejection, reopening, cancellation, and deletion are not implemented.

## Verified database mapping

`dbo.TruckStop.TruckStopID` is a SQL-generated `BIGINT IDENTITY` mapped to `long`. The write boundary accepts only:

- optional `TripID` (`BIGINT`);
- required `TruckID` (`INT`);
- optional `DriverID` (`INT`);
- optional `ShiftID` (`INT`);
- required `StopReasonID` (`INT`);
- required `StartTime` (`DATETIME2(0)`);
- optional `Comments` (`NVARCHAR(500)`).

`EndTime`, computed/persisted `DurationMinutes`, `ValidationStatus`, `CreatedAt`, navigation properties, and the generated entity are absent from HTTP input models. Create explicitly sets `EndTime` to null and `ValidationStatus` to `Pending`; SQL Server generates the key and `CreatedAt`, and computes duration. The trusted constraints permit only Pending, Validated, or Rejected and require an end time not earlier than the start time. No concurrency token exists in the frozen model, so database-update/concurrency failures are converted to safe French messages.

One pre-existing prototype Trip uses identifier `0`; the existing application convention treats HTTP identifiers as positive, so that legacy row is not selectable through this form. No database object or row was changed to address that pre-existing discrepancy.

## Architecture

Application contains dedicated Create/Update commands, `StopEditData`, `StopEditability`, form-option and write-result models, and `IStopWriteService`. Infrastructure implements the service with async EF Core, cancellation tokens, explicit assignments, safe exception handling, and scoped dependency injection. Web contains dedicated Create/Edit input models, a form view model, GET/POST controller actions, French Create/Edit Razor views, a shared form partial, and a small validation/duplicate-submit script partial.

The optional Trip is entered as a numeric identifier rather than loading hundreds of Trip options. Truck, Driver, WorkShift, and StopReason use live, deterministic active-reference dropdowns.

## Validation and workflows

Create validates required fields and the 500-character limit, normalizes whitespace-only comments to null, validates every supplied reference, and verifies the selected Truck plus optional Driver/Shift against the optional Trip. It then maps only authorized fields and redirects to Details after success.

Edit first verifies the route ID equals the hidden input ID, reloads the current row, and requires both `EndTime IS NULL` and `ValidationStatus = Pending`. It revalidates all inputs/references before assigning only the editable fields. The same lock exists inside the write service, so a forged POST cannot bypass the controller check. Closed, Validated, and Rejected rows remain read-only.

Both POST actions use `ValidateAntiForgeryToken`; the controller retains `CanManageStops`. The role matrix was not broadened. Raw exceptions and EF/SQL messages are not displayed.

## Automated and rollback verification

The suite contains 129 tests: 129 passed, 0 failed, 0 skipped. New tests cover input validation/overposting, controller authorization and antiforgery, Create/Edit results, route mismatch, server-side locks, safe conflicts, DI, active options, invalid references, Trip consistency, and SQL rollback behavior.

The real-SQL test captures fingerprints for TruckStop, Trip, Truck, Driver, WorkShift, StopReason, and QualityResult. Inside one explicit transaction it creates a uniquely marked open/Pending stop, verifies SQL-generated/protected fields, edits all allowed fields, proves closed and Validated edits are rejected, and rolls back. After rollback the marker and temporary row are absent and every fingerprint matches.

| Table | Before/after row count | Before/after fingerprint |
|---|---:|---:|
| TruckStop | 150 / 150 | -1898382792 / -1898382792 |
| Trip | 801 / 801 | -1565377314 / -1565377314 |
| Truck | 12 / 12 | -820219747 / -820219747 |
| Driver | 20 / 20 | 371326976 / 371326976 |
| WorkShift | 3 / 3 | -5791193 / -5791193 |
| StopReason | 8 / 8 | -451029055 / -451029055 |
| QualityResult | 120 / 120 | 310868675 / 310868675 |

The live post-test distribution remains 150 Closed/Validated stops and no open/Pending stop.

## Runtime verification

The application ran at `http://localhost:5085`. The headless browser suite passed 30/30 checks: AgentProduction login; Stops list, search, filters, pagination, and details; New Stop action; real reference options; required-field behavior; invalid-reference rejection; Trip/Truck mismatch rejection; locked/missing Edit behavior; anonymous redirects; AgentQualite Access Denied; Voyages regression; connected diagnostics; logout; desktop and 768 px layout; no page overflow; and no console/page errors.

No successful browser Create was submitted because it would persist a test row. Successful writes and the editable-state service flow were proven through the rollback integration test, while controller tests prove the Edit Razor view model is returned for an open/Pending row. Since all 150 live stops are closed/Validated, final human rendering of Edit is recommended after an authorized operational stop exists.

## Quality and safety

Restore, formatting, formatting verification, Debug build, tests, vulnerable-package scan, and repository security scan passed. Final build result: 0 warnings and 0 errors. No vulnerable direct or transitive NuGet package was reported.

No migration or Migrations folder exists. No schema change, raw SQL write, generated-model regeneration/edit, status-history workaround, secret, connection string, browser profile, screenshot, backup, or temporary runtime artifact was introduced. Only `dbo.TruckStop` was written, and only within the rolled-back integration transaction. Port 5085 was free after verification.

## Manual visual acceptance

1. Sign in as an authorized production user and open **Arrêts > Nouvel arrêt**.
2. Confirm the French layout, required indicators, active dropdowns, optional Trip identifier, and Pending/open information note.
3. Submit an incomplete form and confirm field-level messages.
4. During authorized operational use, create a real stop and confirm Details shows it open and Pending.
5. Open **Modifier** for that row, save an allowed correction, and confirm protected fields are unchanged.
6. Confirm the Edit action disappears and direct Edit is refused once the row is closed or no longer Pending.
7. Confirm an unauthorized role receives Access Denied.

## Known limitations

- Part 3 workflows are deliberately absent.
- Optional Trip selection is a validated numeric identifier, not an autocomplete control.
- Human visual acceptance of Edit requires a legitimate open/Pending operational row; no committed disposable test row was created.
- The pre-existing Trip identifier `0` follows an older data convention and remains outside the application’s positive-ID form convention.
