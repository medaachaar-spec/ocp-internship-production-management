# Repository map

Audit date: 2026-07-24.

## Workspace boundary

The Git repository root is `OCP Internship/`. It is on branch `main`, uses the
`origin` remote configured for the private GitHub repository, and currently
contains uncommitted application, test, documentation, reporting-project, and
local research/archive files. The application solution is nested at:

```text
Application/OcpProductionManagement_Sprint0_Starter/OcpProductionManagement_Starter/
```

The current dirty worktree is intentional and must not be reset, cleaned,
stashed, staged, committed, or pushed as part of the presentation-readiness
audit.

## Entrypoints and project boundaries

- Solution: `OcpProductionManagement.sln`
- Web entrypoint: `src/OcpProduction.Web/Program.cs`
- MVC controllers: `src/OcpProduction.Web/Controllers/`
- Database context: `src/OcpProduction.Infrastructure/Data/OcpProductionDbContext.cs`
- Database regeneration helper: `scripts/scaffold-database.ps1`
- Operational SQL assets: repository-level `sql/`
- Reporting SQL assets: repository-level `reporting/SQL/`
- Power BI project: repository-level `reporting/OCP_Mining_Reporting.pbip`

| Project | Responsibility |
|---|---|
| `OcpProduction.Application` | Commands, criteria, DTOs, service contracts, roles, and policy names |
| `OcpProduction.Infrastructure` | EF Core Database-First context, generated entities, SQL-backed services, and configured local authentication |
| `OcpProduction.Web` | ASP.NET Core startup, MVC controllers, Razor views, view models, CSS, and JavaScript |
| `OcpProduction.Tests` | Unit, presentation, authorization, workflow, and SQL integration verification |

Infrastructure depends on Application. Web depends on Application and
Infrastructure. Tests reference all three projects. No background worker,
hosted service, or separate API process was found.

## Route and page inventory

The default conventional route is `{controller=Home}/{action=Index}/{id?}`.
Explicit attribute routes are shown verbatim.

| Method | Route | Policy or access | View / model | Behavior and navigation |
|---|---|---|---|---|
| GET | `/Account/Login` | Anonymous | `Account/Login`, `LoginViewModel` | Read-only; authentication entry |
| POST | `/Account/Login` | Anonymous + anti-forgery | `LoginViewModel` | Creates authentication cookie; rejects non-local return URLs |
| POST | `/Account/Logout` | Authenticated + anti-forgery | Redirect | Signs out; top-bar action |
| GET | `/Account/AccessDenied` | Anonymous | `Account/AccessDenied` | Safe forbidden page |
| GET | `/` or `/Home/Index` | Authenticated | `Home/Index`, `HomeDashboardViewModel` | Policy-gated read-only summary and quick actions |
| GET | `/Home/Error` | Anonymous | `Shared/Error`, `ErrorViewModel` | Safe production error page |
| GET | `/Home/Status?code={code}` | Anonymous | `Home/Status`, integer status | Safe status-code page; not in navigation |
| GET | `/Trips` or `/Trips/Index` | `CanManageTrips` | `Trips/Index`, `TripIndexViewModel` | Read-only filtered list; sidebar |
| GET | `/Trips/Details/{id}` | `CanManageTrips` | `Trips/Details`, `TripDetailsViewModel` | Read-only detail; list/workflow links |
| GET/POST | `/Trips/Create` | `CanManageTrips`; POST anti-forgery | `Trips/Create`, trip form VM | Controlled create; quick action and list |
| GET/POST | `/Trips/Edit/{id}` | `CanManageTrips`; POST anti-forgery | `Trips/Edit`, trip form VM | Controlled edit of editable states |
| GET/POST | `/Trips/Close/{id}` | `CanManageTrips`; POST anti-forgery | `Trips/Close`, `TripCloseViewModel` | GET form, POST closure workflow |
| GET | `/Stops` or `/Stops/Index` | `CanManageStops` | `Stops/Index`, `StopIndexViewModel` | Read-only filtered list; sidebar |
| GET | `/Stops/Details/{id}` | `CanManageStops` | `Stops/Details`, `StopDetailsViewModel` | Read-only detail with policy-gated related links |
| GET/POST | `/Stops/Create` | `CanManageStops`; POST anti-forgery | `Stops/Create`, stop form VM | Controlled create |
| GET/POST | `/Stops/Edit/{id}` | `CanManageStops`; POST anti-forgery | `Stops/Edit`, stop form VM | Controlled edit of open, unvalidated records |
| GET/POST | `/Stops/Close/{id}` | `CanManageStops`; POST anti-forgery | `Stops/Close`, `StopCloseViewModel` | GET form, POST closure workflow |
| GET | `/Quality` or `/Quality/Index` | `CanManageQuality` | `Quality/Index`, quality index VM | Read-only filtered list; sidebar |
| GET | `/Quality/Details/{id}` | `CanManageQuality` | `Quality/Details`, quality detail VM | Read-only factual detail |
| GET/POST | `/Quality/Create` | `CanManageQuality`; POST anti-forgery | `Quality/Create`, quality form VM | Controlled create without invented conformity |
| GET/POST | `/Quality/Edit/{id}` | `CanManageQuality`; POST anti-forgery | `Quality/Edit`, quality form VM | Controlled correction of editable records |
| GET | `/Validation/Trips` | `CanValidate` | `Validation/Index`, pending-trip VM | Pending trips only; validation center |
| POST | `/Validation/Trips/{id}/Validate` | `CanValidate` + anti-forgery | Redirect | Validates one pending trip |
| GET | `/Validation/Stops` | `CanValidate` | `StopValidation/Index`, pending-stop VM | Pending stops only; linked from validation center |
| POST | `/Validation/Stops/{id}/Validate` | `CanValidate` + anti-forgery | Redirect | Validates one pending stop |
| GET | `/Trucks` or `/Trucks/Index` | Fleet role set | `Trucks/Index`, truck index VM | Read-only fleet list; sidebar |
| GET | `/Trucks/Details/{id}` | Fleet role set | `Trucks/Details`, truck detail VM | Read-only factual detail |
| GET | `/Drivers` or `/Drivers/Index` | Fleet role set | `Drivers/Index`, driver index VM | Read-only fleet list; sidebar |
| GET | `/Drivers/Details/{id}` | Fleet role set | `Drivers/Details`, driver detail VM | Read-only factual detail |
| GET | `/Dashboard` | `CanViewDashboard` | `Dashboard/Index`, `DashboardViewModel` | Six KPI cards, five charts, inclusive date filter |
| GET | `/Performance/Trucks` | `CanViewReporting` | `Performance/Trucks`, truck performance VM | Full filtered dataset for cards/charts; paged table |
| GET | `/Performance/Trucks/{id}` | `CanViewReporting` | `Performance/TruckDetails`, detail VM | Read-only detail preserving return criteria |
| GET | `/Performance/Drivers` | `CanViewReporting` | `Performance/Drivers`, driver performance VM | Neutral association wording; paged table |
| GET | `/Performance/Drivers/{id}` | `CanViewReporting` | `Performance/DriverDetails`, detail VM | Read-only detail preserving return criteria |
| GET | `/ReferenceData` | `CanAdminister` | `ReferenceData/Index`, catalog VM | Implemented reference-data overview; sidebar |
| GET/POST | `/ReferenceData/WorkShifts[/Create|/{id}/Edit|/{id}/Active]` | `CanAdminister`; POST anti-forgery | Work-shift list/form VMs | Controlled create, edit, activate/deactivate |
| GET/POST | `/ReferenceData/LoadingPoints[/Create|/{id}/Edit|/{id}/Active]` | `CanAdminister`; POST anti-forgery | Loading-point list/form VMs | Controlled create, edit, activate/deactivate |
| GET/POST | `/ReferenceData/Destinations[/Create|/{id}/Edit|/{id}/Active]` | `CanAdminister`; POST anti-forgery | Destination list/form VMs | Controlled create, edit, activate/deactivate |
| GET/POST | `/ReferenceData/StopReasons[/Create|/{id}/Edit|/{id}/Active]` | `CanAdminister`; POST anti-forgery | Stop-reason list/form VMs | Controlled create, edit, activate/deactivate |
| GET | `/Diagnostics` or `/Diagnostics/Index` | `CanViewTechnicalDiagnostics` | `Diagnostics/Index`, `DiagnosticsViewModel` | Read-only runtime checks; administrator sidebar |

No implemented controller is orphaned. Create, edit, detail, closure, validation,
and reference subroutes are intentionally reached contextually rather than by a
dedicated sidebar entry. The status and error routes are infrastructure routes
and are intentionally absent from navigation.

## Role and policy map

| Feature | Administrateur | Superviseur | ResponsableProduction | AgentProduction | AgentQualite |
|---|---:|---:|---:|---:|---:|
| Home | Yes | Yes | Yes | Yes | Yes |
| Trips and trip closure | No | Yes | No | Yes | No |
| Stops and stop closure | No | Yes | No | Yes | No |
| Quality | No | Yes | No | No | Yes |
| Trip and stop validation | No | Yes | No | No | No |
| Trucks and drivers | Yes | Yes | Yes | Yes | No |
| Dashboard | Yes | Yes | Yes | No | No |
| Truck/driver performance | No | Yes | Yes | No | No |
| Reference data | Yes | No | No | No | No |
| Technical diagnostics | Yes | No | No | No | No |

The sidebar evaluates the existing policies for reporting, dashboard,
administration, and diagnostics. Operational sections match the corresponding
role-backed policies. Administrator access is not broadened into operational
validation.

## Principal data flows

```text
Cookie-authenticated request
  → MVC controller policy
  → Application service contract
  → Infrastructure service
  → OcpProductionDbContext
  → generated tables or keyless reporting entities
  → DTO / view model
  → Razor view
```

Reporting pages use `AsNoTracking` queries and do not call `SaveChanges`.
Dashboard and performance pages serialize typed DTOs with `System.Text.Json`;
the default safe encoder protects the JSON embedded in `application/json`
script elements.

Write workflows bind explicit input models, construct explicit commands,
validate referenced records server-side, protect state-changing forms with
anti-forgery tokens, and handle database failures without rendering raw
exceptions.

## Database and environment touchpoints

- Database: `OCP_Mining_Prototype`.
- Model style: EF Core Database-First with a frozen schema.
- Generated entities and context mappings: Infrastructure `Generated/` and
  `OcpProductionDbContext.cs`; they are not application-edit targets.
- Reporting definitions: repository `reporting/SQL/`; no reporting service
  writes to them.
- Local database connection and user credentials: ASP.NET Core User Secrets,
  outside the repository.
- Local launch profiles: `src/OcpProduction.Web/Properties/launchSettings.json`.
- No migration, publish profile, container manifest, cloud deployment
  manifest, or CI workflow is part of the current application.

## Frontend touchpoints

- Shared layout and role-aware navigation: `Views/Shared/`.
- Shared visual system and responsive behavior: `wwwroot/css/site.css`.
- Performance-page visual system: `wwwroot/css/performance.css`.
- Shared Chart.js configuration: `wwwroot/js/chart-theme.js`.
- Page scripts: `dashboard.js`, `truck-performance.js`,
  `driver-performance.js`.
- Chart.js is pinned to 4.4.7. Accessible fallback tables remain available
  when charts cannot render.

## Test map

Tests are grouped under Authentication, Dashboard, Diagnostics, Fleet, Home,
Infrastructure, Performance, Presentation, Quality, ReferenceData, Security,
Stops, and Trips. Tests marked `Category=DatabaseIntegration` require the
compatible local SQL database; write tests preserve state with rollback and
fingerprint checks.

The 2026-07-24 baseline audit passed 268 database-independent tests and 17 SQL
integration tests before final presentation cleanup. The SQL assertions confirm
801 trips, 54,533.80 completed tonnes, 150 stops, 24,984 stop minutes, 120
quality results, 0 pending trips, 12 trucks, 20 drivers, and no TruckStop 153.

## Non-application workspace areas

| Area | Contents | Publication note |
|---|---|---|
| `docs/` | Functional, technical, and historical documents | Preserve history; review binary documents for confidentiality |
| `Research/` | External research material | Verify provenance, licensing, and confidential content |
| `backups/` | Historical local copies | Unrelated to the final application commit; preserve until manually reviewed |
| `Power BI/` | Power BI documentation | Keep credentials and imported confidential data out of Git |
| `reporting/` | SQL reporting assets and PBIP project | Review generated `.pbi/` cache exclusions before publication |
| `tmp/` and build outputs | Ignored generated files | Do not stage |

## Highest-leverage future improvements

1. Complete the authenticated manual viewport, keyboard, and 200% zoom matrix
   before the defense.
2. Add CI for restore, warning-free build, database-independent tests, and
   secret scanning after the internship presentation.
3. Define a reviewed non-prototype identity solution before any production
   deployment; do not treat the configured local provider as production auth.
4. Establish a deployment-specific secret store, TLS policy, backup policy,
   and operational ownership before deployment work.
5. Add automated browser accessibility checks when an approved browser test
   environment is available.
6. Decide whether Bootstrap and Chart.js require approved local fallbacks for
   offline demonstrations.
7. Review all PDFs, research files, database exports, and Power BI imported
   data for confidentiality before the repository is shared.
8. Add a release checklist covering manual role checks, invalid routes,
   blocked-chart behavior, SQL failure behavior, and screenshot capture.
9. Add a documented database refresh procedure that preserves the frozen
   schema and validated reporting definitions.
10. Reassess dependency updates in a separate, fully tested maintenance task;
    do not combine them with presentation cleanup.
