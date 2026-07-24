# Sprint 3 Quality execution report

## Scope and outcome

Sprint 3 implements the compatible Quality scope against the frozen SQL Server database:

- read-only list, server-side filters, pagination, and details;
- creation and correction of verified writable measurement fields;
- role-aware navigation and `CanManageQuality` authorization;
- rollback-only successful write verification.

`Quality validation workflow: NOT APPLICABLE`. The database’s status values are analytical classifications and do not define a genuine validation lifecycle. No fake queue, final state, transition, audit field, or Quality-specific `CanValidate` action was added.

## Database compatibility

`dbo.QualityResult` has a `BIGINT IDENTITY` primary key mapped to `long`. Existing identities include zero. The table contains required `AnalysisDate`, required `decimal(10,3)` `QualityValue`, SQL-defaulted `QualityUnit = P2O5Percent`, nullable `TargetValue`, SQL-defaulted `Status = Pending`, optional `Comments`, and SQL-defaulted `CreatedAt`. `TripID` and `LoadingPointID` are optional foreign keys, but the trusted source constraint requires at least one.

The trusted Quality checks permit non-negative measurement/target values and the statuses `Review`, `Pending`, `AboveTarget`, `BelowTarget`, and `InTarget`. Current data contains 18 AboveTarget, 19 BelowTarget, 80 InTarget, and 3 Review rows; no Pending row currently exists.

`dbo.vw_QualityAnalysis` is generated as keyless `vw_QualityAnalysis` through DbSet `vw_QualityAnalyses`. It supplies the verified Voyage, truck, driver, shift, loading-point, destination, date, measurement, unit, target, and stored-status context used by the read module.

No table, view, column, key, relationship, constraint, index, or data row was structurally changed. Generated entities and `OcpProductionDbContext` were not edited or regenerated. No migration was created.

## Architecture

Application contains dedicated Quality DTOs, normalized criteria, compact filter/form options, actual status definitions, commands, edit data, write results, editability rule, and read/write service interfaces. The existing `PagedResult<T>` is reused.

Infrastructure contains:

- `QualityReadService`: no-tracking, async, server-filtered, directly projected EF Core queries with deterministic ordering and pagination;
- `QualityWriteService`: explicit entity creation, tracked updates, source consistency checks, database-compatible decimal validation, protected fields, safe French errors, and no raw SQL write.

Web contains:

- `QualityController` under `CanManageQuality`;
- GET list/details/create/edit and antiforgery-protected POST create/edit;
- narrow input models rather than generated EF entities;
- French presentation/navigation helpers and responsive Bootstrap views;
- role-aware sidebar entry and `CanManageTrips`-checked Voyage links.

## Read-only behavior

The list searches verified result, truck, driver, loading-point, and destination context. Date, Voyage, truck, P₂O₅ range, and stored-status filters execute in SQL Server. Page sizes are 10, 20, and 50. Results order by newest `AnalysisDate`, then descending result ID.

Details combines the view context with the base row to show the actual measurement, unit, stored target, status, observation, and creation timestamp. Stored comparison statuses are displayed neutrally. No conformity verdict or business threshold is calculated.

## Creation and correction safety

The HTTP input surface contains only:

- `AnalysisDate`;
- optional `TripId`;
- optional `LoadingPointId`;
- `QualityValue`;
- optional `Comments`.

At least one source is required. A supplied Voyage and loading point must both exist and match. P₂O₅ accepts French comma or browser decimal-point input and must fit `decimal(10,3)`, remain non-negative, and not require value-changing rounding. No arbitrary 0–100 business range or threshold was added.

Create leaves identity, unit, target, status, and creation timestamp to verified SQL defaults. Edit reloads the tracked row and changes only the five authorized fields. Route and form IDs must match. Protected properties are absent from both input models. There is no `DbSet.Update`, deletion, related-table update, raw SQL write, or raw exception response.

Because the current status values do not identify validation, `QualityEditability` documents and applies the safest supported prototype rule: recorded measurements remain correctable. A future validated-row lock requires a real database-backed lifecycle.

## Validation phase decision

Validated functional documents describe Quality submission/validation conceptually, but the frozen schema has no final validated value, no allowed transition to such a value, and no validator/date/history fields. The six mandatory compatibility conditions are not met. Phase D was therefore skipped and no validation commit was created.

## Automated verification

The final suite contains 191 tests: 191 passed, 0 failed, 0 skipped. Quality coverage includes:

- criteria normalization, zero-valued identity support, bounds, paging, and empty results;
- French date/decimal/unit and actual-status presentation without inferred conformity;
- controller policy, authorized Voyage links, antiforgery, route/form ID checks, ModelState, success redirects, and protected input boundaries;
- French/browser decimal parsing and input validation;
- live read-only list/detail/filter/pagination verification with unchanged fingerprint;
- live create/update verification inside an explicit transaction rolled back in `finally`.

The write integration test creates a uniquely marked row, verifies its generated key and SQL defaults, changes only allowed fields, confirms protected fields remain identical, rejects missing/incompatible references and excessive precision, checks related tables, and rolls back. The row and marker are absent afterward.

Final command results:

- restore: passed;
- format: passed;
- format verification: passed;
- Debug build: 0 warnings, 0 errors;
- tests: 191 passed, 0 failed, 0 skipped;
- vulnerable packages: none reported.

## Runtime verification

The app ran at `http://localhost:5085`. A direct local Edge/Playwright suite passed 49 of 49 checks with no console or page errors. The bundled persistent browser kernel could not initialize because its packaged assets path was unavailable, so the same bundled Playwright dependency was run directly without installing packages.

Verified behavior includes:

- anonymous redirect and login/logout;
- AgentQualite sidebar/list/filter/pagination/empty-state/details/Create/Edit access;
- actual 120-row count and combined date/Voyage/truck/P₂O₅/status filters;
- authorized/unauthorized related Voyage link behavior;
- French incomplete, missing-source, missing-reference, and excessive-precision errors;
- missing-antiforgery rejection;
- protected Create/Edit field absence;
- AgentProduction Quality denial;
- Superviseur Quality/Voyages/Stops/Validation Center access;
- connected Diagnostics;
- desktop and approximately 768px visual layout with no page-level overflow;
- absence of a nonexistent Quality validation feature.

No successful browser write was submitted because that would persist operational data. Successful writes are covered by the rollback integration test. All task-owned browser/app processes were stopped and port 5085 is free.

## Database safety snapshot

The same explicit fingerprint formulas were used around rollback tests. Final values are:

| Table | Rows | Fingerprint |
|---|---:|---:|
| QualityResult | 120 | 310868675 |
| Trip | 801 | -1565377314 |
| Truck | 12 | -820219747 |
| Driver | 20 | 371326976 |
| WorkShift | 3 | -5791193 |
| LoadingPoint | 4 | 4472832 |
| Destination | 3 | 1155323188 |
| TruckStop | 151 | -2042191468 |

Rollback marker count is zero. No uncontrolled test row, migration, generated-model change, schema modification, or connection-string exposure exists.

## Security review

All Quality endpoints inherit `CanManageQuality`. Both write POST actions use `ValidateAntiForgeryToken`; generated entities are never HTTP-bound; protected fields are absent from inputs; Edit independently reloads current state and verifies route/form IDs; persistence errors become safe French messages; and no raw exception is rendered.

Repository scans found no real SQL connection string, password, API key, or token. The README contains placeholder connection syntax and intentionally documented local demo credentials only. The actual `ConnectionStrings:OcpDatabase` value remains in .NET User Secrets and was neither displayed nor committed.

No role or policy matrix changed. No conformity rule, P₂O₅ threshold, validation workflow, or comments-based audit history was invented.

## Git checkpoints

- `c852ff8` — `Implement Sprint 3 read-only Quality module`
- `01f8e65` — `Implement Sprint 3 Quality creation and editing`
- no Phase D commit, because validation is not applicable

Documentation is finalized in a separate documentation-only checkpoint.

## Known limitations

- The frozen schema cannot distinguish draft, submitted, and validated Quality records.
- There is no rowversion or other strong concurrency token; tracked updates safely handle deletion/update exceptions but cannot detect every last-write-wins case.
- No validator identity, validation date, validation history, rejection, correction request, or reopening mechanism exists.
- Current TargetValue and analytical statuses are displayed as stored; the application does not define their business authority.
- Successful browser writes are intentionally excluded to preserve the database; rollback integration tests verify them.

## Final manual acceptance checklist

1. Review the list, detail, Create, and Edit screens in the project owner’s normal browser and display scaling.
2. Perform a permanent Create/Edit only with an authorized legitimate production measurement.
3. Confirm the future Quality validation lifecycle and controlled database change with the data owner before implementation.
