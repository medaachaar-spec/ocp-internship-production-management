# Sprint 2 Part 3 execution report

## Scope and outcome

Sprint 2 Part 3 completes the authorized Truck Stop workflow:

```text
Open + Pending -> Closed + Pending -> Closed + Validated
```

An AgentProduction or Superviseur with `CanManageStops` may close an eligible stop. Only a Superviseur with `CanValidate` may validate it. The existing role and policy matrix was not changed. Rejection, correction requests, reopening, deletion, cancellation, validation history, validator identity, and validation date remain intentionally outside scope.

## Frozen database compatibility

Read-only metadata verification confirmed:

- `dbo.TruckStop` exists with `TruckStopID` mapped from `BIGINT` to `long`;
- `StartTime` is required and `EndTime` is nullable `DATETIME2(0)`;
- `DurationMinutes` remains a persisted computed column derived by `DATEDIFF(minute, StartTime, EndTime)`;
- `ValidationStatus` remains required `NVARCHAR(20)` with SQL default `Pending`;
- `CK_TruckStop_EndTime` permits only a null EndTime or an EndTime at/after StartTime;
- `CK_TruckStop_ValidationStatus` permits `Pending`, `Validated`, and `Rejected`;
- both check constraints are enabled and trusted.

No database object was modified. No migration was created. Generated entities and `OcpProductionDbContext` were not regenerated or edited.

## Architecture

Application contains a dedicated `CloseStopCommand`, closure and pending-queue DTOs, normalized validation criteria, workflow results, centralized `StopWorkflowRules`, and `IStopWorkflowService`. Existing `StopEditability` delegates to the centralized rules so Part 2 editing and Part 3 transitions share one lock definition.

Infrastructure implements `StopWorkflowService` with async EF Core, cancellation tokens, explicit field assignments, safe French error results, server-side filters/pagination, and scoped dependency injection. It performs no raw SQL write.

Web contains:

- `StopWorkflowController` for GET/POST `/Stops/Close/{id}` under `CanManageStops`;
- `StopValidationController` for GET `/Validation/Stops` and POST `/Validation/Stops/{id}/Validate` under `CanValidate`;
- an EndTime-only closure input/view;
- a French supervisor pending-stop queue with search, closure-date, truck, reason, and page-size filters;
- status-aware Details actions, validation-center tabs, active navigation, and supervisor dashboard counts.

## Workflow and transition safety

Closure is accepted only when EndTime is null and ValidationStatus is exactly `Pending`. It requires a non-default EndTime at or after StartTime and assigns only `EndTime`. The SQL-computed duration is reloaded from the database when read; the application never writes it.

Validation is accepted only when EndTime is non-null and ValidationStatus is exactly `Pending`. It assigns only `ValidationStatus = Validated`. Open, already Validated, Rejected, missing, repeated, and forged transitions are rejected with safe French messages.

Ordinary Edit remains possible only while the stop is Open/Pending. Closed/Pending and Validated stops are locked in the controller and again in the write service, so a forged Edit POST cannot bypass the rule.

No HTTP input accepts ValidationStatus, DurationMinutes, CreatedAt, or another protected field. Closure and validation POST actions use `ValidateAntiForgeryToken`; validation accepts only the route identifier.

## Supervisor queue

The queue includes only stops where `EndTime IS NOT NULL` and `ValidationStatus = Pending`. Search and filters execute server-side. Date filters apply to closure time with an inclusive selected end date, page sizes are limited to 10, 20, and 50, and ordering is oldest EndTime first then TruckStopID.

The queue links to the existing stop Details page for review. The validation action appears only to a `CanValidate` user when the current stop is closed/Pending. The Validation Center preserves the existing Voyages queue and adds a distinct Arrêts queue.

## Automated and rollback verification

The full suite contains 145 tests: 145 passed, 0 failed, 0 skipped. New tests cover:

- the workflow-rule truth table and existing editability delegation;
- closure input validation and overposting boundary;
- endpoint routes, policies, antiforgery, and absence of forbidden actions;
- GET/POST closure results, route/form mismatch, invalid ModelState, and safe redirects;
- supervisor queue criteria and validation behavior;
- real SQL Server closure, computed duration, queue filtering/ordering, validation, repeated-transition rejection, and edit locks.

The SQL integration test captures seven table fingerprints, creates two uniquely marked temporary stops inside one explicit transaction, executes the full workflow, and rolls back in `finally`. After rollback, both temporary rows and markers are absent and every captured fingerprint matches.

| Table | Before/after row count | Before/after fingerprint |
|---|---:|---:|
| TruckStop | 151 / 151 | -1070686150 / -1070686150 |
| Trip | 801 / 801 | -1565377314 / -1565377314 |
| Truck | 12 / 12 | -820219747 / -820219747 |
| Driver | 20 / 20 | 371326976 / 371326976 |
| WorkShift | 3 / 3 | -5791193 / -5791193 |
| StopReason | 8 / 8 | -451029055 / -451029055 |
| QualityResult | 120 / 120 | 310868675 / 310868675 |

The current live distribution is 150 Closed/Validated stops and one pre-existing Open/Pending stop. Part 3 did not create or permanently change that row.

## Runtime verification

The application ran at `http://localhost:5085`. Browser and direct HTTP checks covered anonymous redirects; AgentProduction login/logout, role navigation, list/filter/details, closure action, closure form boundaries, invalid EndTime rejection, antiforgery rejection, and validation denial; Superviseur dashboard, both validation queues, stop queue filters/empty state, validation antiforgery, invalid-state rejection, connected Diagnostics, and safe database identity; AgentQualite Access Denied; desktop and 768 px overflow; and browser console/page errors.

There were 44 applicable workflow/browser checks. The first diagnostic harness assertion expected the English word `Connected`; the page correctly renders the French label `Connectée`. The assertion was corrected and the diagnostic subset passed 4/4. This was a test-language mismatch, not an application defect.

No successful browser closure or validation was submitted because that would persist operational data. Successful transitions and their rendered model boundaries are covered by controller tests and the rollback SQL integration test. After runtime verification, the database fingerprint remained unchanged and port 5085 was free.

## Quality and security

Restore, formatting, formatting verification, Debug build, tests, vulnerable-package scan, repository secret scan, migration scan, generated-file diff, raw-SQL scan, and `git diff --check` passed. The build produced 0 warnings and 0 errors. NuGet reported no vulnerable direct or transitive package.

Only placeholder connection syntax exists in the README. The actual `ConnectionStrings:OcpDatabase` value remains in .NET User Secrets and was not printed or committed. No raw exception, stack trace, credential, or connection string is exposed by the workflow or Diagnostics page.

## Manual visual acceptance recommendation

1. During authorized operational use, close a legitimate Open/Pending stop and visually confirm it appears in **Centre de validation > Arrêts à valider**.
2. As Superviseur, review and validate that legitimate row, then confirm the Details page displays Clôturé/Validé with no Edit, Close, or Validate action.
3. Confirm the populated queue layout and pagination with several legitimate closed/Pending stops.

## Known limitations

- The frozen schema has no validator identity, validation date, audit history, correction comment, or reopening history.
- No rejection, correction, reopening, deletion, cancellation, batch validation, or notification workflow is implemented.
- The browser smoke test intentionally avoids persistent successful workflow writes; successful transitions are verified inside rollback integration transactions.
- The in-app browser runtime could not initialize because its packaged kernel-assets path was unavailable; the same checks ran with the bundled local Edge/Playwright runtime.
- Sprint 3 has not started.
