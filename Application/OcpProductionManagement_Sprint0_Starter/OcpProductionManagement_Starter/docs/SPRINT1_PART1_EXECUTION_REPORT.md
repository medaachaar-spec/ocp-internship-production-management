# Sprint 1 Part 1 Execution Report

Finalization date: 2026-07-20

## Scope delivered

Sprint 1 Part 1 implements the read-only Voyages module only:

- SQL-backed voyage list;
- free-text search and combined server-side filters;
- deterministic server-side pagination;
- read-only voyage details;
- filter-preserving navigation;
- role-aware sidebar integration;
- `CanManageTrips` server authorization;
- focused automated tests and runtime verification.

No creation, modification, closure, cancellation, validation, export, API, Arrêts module, or other write workflow was added.

## Sources reviewed

The implementation was aligned with the available workspace documentation:

- `Specification_cas_utilisation_OCP_Phosboucraa.pdf`;
- `Arborescence_menus_navigation_OCP_Phosboucraa.pdf`;
- `Inventaire_ecrans_prototype_OCP_Phosboucraa.pdf`;
- `Specifications_UIUX_Maquettes_Ecrans_Prioritaires_OCP_Phosboucraa.pdf`;
- `Architecture_technique_et_Sprint_0_OCP_Phosboucraa.pdf`;
- `Structure_Base_Donnees_OCP_Mining_Prototype.pdf`;
- `Référentiel Fonctionnel Cible du Prototype.pdf`;
- `Note de Cadrage Fonctionnelle.pdf`;
- `data_dictionary.md`;
- `database_design.md`;
- `data_assumptions.md`;
- `source_mapping.md`;
- Sprint 0 checklist and final execution reports.

`Architecture_fonctionnelle_globale_OCP_Phosboucraa.pdf` is not present in the workspace. The available validated functional reference, use-case, navigation, screen-inventory, and UI/UX documents were used instead.

## Actual database and generated-model mapping

List source:

- SQL view: `dbo.vw_TripSummary`;
- generated type: `vw_TripSummary`;
- generated DbSet: `OcpProductionDbContext.vw_TripSummaries`;
- mapping: keyless entity (`HasNoKey`).

Detail source:

- SQL view: `dbo.vw_TripDetails`;
- generated type: `vw_TripDetail`;
- generated DbSet: `OcpProductionDbContext.vw_TripDetails`;
- mapping: keyless entity (`HasNoKey`);
- joined read-only to `vw_TripSummary` for associated aggregates and `Trip` for `CreatedAt`.

| UI field | Generated source | Actual C# property | Available |
|---|---|---|---|
| Voyage ID | Both views | `TripID` (`long`) | Yes |
| Date | Both views | `TripDate` (`DateOnly`) | Yes |
| Camion | Both views | `TruckID`, `TruckCode` | Yes |
| Conducteur | Summary / detail | `DriverID`, `DriverCode`, `FullName` / `DriverName` | Yes |
| Poste | Both views | `ShiftID`, `ShiftCode`, `ShiftName` | Yes |
| Chargement | Both views | `LoadingPointID`, `LoadingPointCode`, `LoadingPointName` | Yes |
| Destination | Both views | `DestinationID`, `DestinationCode`, `DestinationName` | Yes |
| Début | Both views | `StartTime` | Yes |
| Fin | Both views | `EndTime` | Yes, nullable |
| Durée | Summary / detail | `TripDurationMinutes` / `DurationMinutes` | Yes, nullable |
| Tonnage | Both views | `Tonnage` | Yes, nullable |
| Statut | Both views | `Status` | Yes |
| Observation | Detail view | `Comments` | Yes, nullable |
| Création | `Trip` | `CreatedAt` | Yes |
| Arrêts associés | Summary view | `StopCount`, `TotalStopDurationMinutes` | Yes |
| Qualité associée | Summary view | `QualityResultCount`, `LatestP2O5` | Yes |
| Code voyage métier | None | None | No; numeric `TripID` is displayed |
| Auteur / modification | None | None | No; omitted |
| État/date de validation | None | None | No; omitted |

The actual identifier type is `long`. The actual table is `WorkShift`, while older technical material refers to `Shift`; the generated and frozen database name is used.

The database contains 800 voyages from 2026-06-01 through 2026-06-30. Actual statuses are `Completed`, `Incomplete`, and `InProgress`; the Web layer renders documented French labels and uses a neutral badge for any unknown future value.

## Architecture

Application:

- `TripListItemDto` and `TripDetailsDto` expose presentation-safe read data;
- `TripSearchCriteria` normalizes search text, identifiers, page, and page size;
- `TripFilterOptions` provides truck, driver, shift, and status choices;
- `PagedResult<T>` handles totals and navigation safely, including zero results;
- `ITripReadService` defines list, details, and filter-option operations.

Infrastructure:

- `TripReadService` composes EF Core queries against the verified views;
- `DependencyInjection` registers `ITripReadService` as scoped.

Web:

- `TripsController` contains GET-only thin actions;
- `TripIndexViewModel` and `TripDetailsViewModel` preserve query state;
- `TripPresentation` centralizes status labels, badge classes, and duration formatting;
- Razor list and detail views use the existing Bootstrap visual system;
- the existing sidebar exposes Voyages only to AgentProduction and Superviseur.

The Application project has no Infrastructure dependency, generated models are not returned to Web, and generated files were not edited.

## Query behavior and performance

- `AsNoTracking` is applied to every query source.
- Search uses parameterized `EF.Functions.Like` with escaped wildcard characters.
- Search covers numeric voyage ID, truck code, driver code/name, loading point, and destination.
- `DateFrom` and `DateTo` are inclusive on the operational `TripDate`.
- Truck, driver, work-shift, and status filters are combinable.
- Filter options are distinct, ordered, and limited to values present in voyages.
- Filtering occurs before `CountAsync` and materialization.
- Ordering is deterministic: `StartTime` descending, then `TripID` descending.
- Pagination uses SQL-side `Skip` and `Take` with page sizes 10, 20, or 50.
- List and detail operations project directly to DTOs.
- Details use one joined SQL query; no `Include`, lazy loading, N+1 query, dynamic SQL, or full-table in-memory filtering is used.

## UI behavior

- French page title and description;
- accessible GET filter form with persistent labels;
- selected filters remain visible after requests;
- concise result count with singular/plural wording;
- responsive table with an internal horizontal scroll area on narrow screens;
- explicit empty state and filter-reset link;
- nearby-page pagination links with previous/next controls;
- detail links preserve all filters, page, and page size;
- read-only detail sections: general information, assignment, route, production, associated data, and observations;
- no nonfunctional or write-oriented action is rendered.

## Authorization

`TripsController` uses the existing `CanManageTrips` policy. The role matrix was not modified:

- AgentProduction: authorized;
- Superviseur: authorized;
- AgentQualite: denied;
- ResponsableProduction: denied by the current validated application policy;
- Administrateur: denied by the current validated application policy;
- anonymous: redirected to login.

Some older functional documents propose read-only access for ResponsableProduction and Administrateur. The existing policy and the explicit Sprint 1 instruction take precedence for this delivery; access was not broadened.

## Automated verification

Added coverage includes:

- invalid page normalization;
- supported and unsupported page sizes;
- whitespace normalization;
- identifier-filter normalization;
- total-page and zero-result calculations;
- controller policy and GET-only actions;
- normalized controller input;
- existing and missing detail results;
- route-state and detail-identifier preservation;
- Infrastructure DI resolution.

Final results:

- restore: passed;
- formatting and formatting verification: passed;
- build: 0 warnings, 0 errors;
- tests: 26 passed, 0 failed, 0 skipped;
- vulnerable package audit: no vulnerable direct or transitive package reported.

## Runtime verification

The application was tested at `http://localhost:5085` against the real frozen database:

- AgentProduction login and Voyages menu: passed;
- default list and real 800-row result count: passed;
- identifier and text search: passed;
- individual date, truck, driver, work-shift, and status filters: passed;
- combined filters and selected-value persistence: passed;
- page 2, page size 10, and pagination query preservation: passed;
- reset and empty state: passed;
- valid detail and invalid-ID 404: passed;
- no write action rendered: passed;
- anonymous redirect and AgentQualite Access Denied behavior: passed;
- Diagnostics connected state: passed;
- antiforgery logout and cookie removal: passed;
- total runtime assertions: 22 passed, 0 failed.

An isolated headless Edge visual check confirmed:

- meaningful desktop list and detail content;
- all eight filter controls have associated labels;
- active Voyages navigation;
- working real detail link;
- expected detail sections and return navigation;
- no write actions;
- no error overlay, browser exception, or console error;
- no page-level horizontal overflow at 768 px;
- the wide table remains independently horizontally scrollable.

All application and browser processes created for verification were stopped, ports were released, and temporary screenshots were removed.

## Database and security safety

- No database write command or write API was used.
- The `Trip` table count/checksum fingerprint was identical before and after runtime testing.
- No `SaveChanges`, Add, Update, Remove, ExecuteSql, EnsureCreated, Migrate, POST action, or raw SQL exists in the Voyages module.
- No migration directory or command exists.
- No schema object or generated model was changed or regenerated.
- The real connection string remains in .NET User Secrets.
- Repository scans found no copy of the stored secret or generic credential/token pattern in the new module.
- Razor encoding remains enabled; no `Html.Raw` is used for database content.

## Known limitations

- No business voyage code exists; the numeric `TripID` is used.
- Author, modification, and validation-history fields do not exist in the frozen schema and are omitted.
- User-selectable column sorting is not part of Part 1; deterministic newest-first ordering is used.
- Associated stops and quality are summarized by verified counts/values rather than separate module pages.
- Creation, editing, closure, cancellation, submission, validation, and export remain intentionally unavailable.

## Git checkpoint

This report is included in the authorized local checkpoint named `Implement Sprint 1 read-only Voyages list and details`. No remote or push is part of this delivery.

## Next recommended task

Sprint 1 Part 2 - implement Voyage creation and editing after explicit authorization.
