# Repository map

Audit date: 2026-07-24.

## Workspace boundary

The intended publication boundary is `OCP Internship/`. It is not currently a Git repository. One existing repository is nested at:

```text
Application/OcpProductionManagement_Sprint0_Starter/OcpProductionManagement_Starter/
```

That repository is on branch `main`, has no configured remote, and contains uncommitted Sprint 5 dashboard and authorization work.

## Entrypoints

- Solution: `Application/OcpProductionManagement_Sprint0_Starter/OcpProductionManagement_Starter/OcpProductionManagement.sln`
- Web entrypoint: `src/OcpProduction.Web/Program.cs`
- MVC routes: controllers under `src/OcpProduction.Web/Controllers/`
- Database model: `src/OcpProduction.Infrastructure/Data/OcpProductionDbContext.cs`
- Database scaffolding helper: `scripts/scaffold-database.ps1`
- SQL database creation flow: `sql/01_create_database.sql` through `sql/05_validation_queries.sql`
- Reporting layer: `reporting/01_vw_TripSummary.sql` through `reporting/07_validate_reporting_layer.sql`

## Project boundaries

| Project | Responsibility |
|---|---|
| `OcpProduction.Application` | Contracts, commands, criteria, DTOs, service interfaces, roles, and policy names |
| `OcpProduction.Infrastructure` | EF Core Database-First context and entities, SQL-backed services, demo authentication |
| `OcpProduction.Web` | ASP.NET Core startup, MVC controllers, Razor views, view models, CSS, and JavaScript |
| `OcpProduction.Tests` | Unit, presentation, authorization, service, workflow, and SQL integration tests |

Project references flow from Web to Application and Infrastructure, and from Infrastructure to Application. The test project references all three application projects.

## Feature areas

- authentication and policy authorization;
- dashboard;
- diagnostics;
- trips and trip workflow;
- truck stops and stop workflow;
- quality;
- trucks and drivers;
- performance reporting;
- reference-data administration.

## Dashboard data flow

```text
GET /Dashboard
  → DashboardController
  → IDashboardReadService
  → DashboardReadService
  → OcpProductionDbContext keyless reporting entities
  → DashboardDto
  → DashboardViewModel
  → Views/Dashboard/Index.cshtml
  → page-scoped dashboard.js and Chart.js
```

The query service uses `AsNoTracking` and date-filters the trip, stop, and quality reporting views. No dashboard call to `SaveChanges` was found.

## Database and reporting touchpoints

- Database: `OCP_Mining_Prototype`.
- Model style: EF Core Database-First.
- Schema rule: frozen; no routine migrations.
- Operational SQL scripts: `sql/`.
- Dedicated reporting definitions and validation: `reporting/`.
- Scaffolded keyless view entities include trip, stop, quality, daily, reason, global, executive, truck, and driver reporting views.
- A local User Secrets identifier is configured for the Web project; its current secrets file is outside the workspace.

## Frontend touchpoints

- Shared layout and sidebar: `src/OcpProduction.Web/Views/Shared/`.
- Application styles: `src/OcpProduction.Web/wwwroot/css/site.css`.
- Dashboard JavaScript: `src/OcpProduction.Web/wwwroot/js/dashboard.js`.
- Dashboard rendering: `src/OcpProduction.Web/Views/Dashboard/Index.cshtml`.

## Tests

Tests are grouped under:

- `Dashboard`;
- `Fleet`;
- `Infrastructure`;
- `Performance`;
- `Quality`;
- `ReferenceData`;
- `Security`;
- `Stops`;
- `Trips`.

SQL integration tests depend on a reachable local SQL Server and exact validated data baselines. The current database contains 150 stops after an authorized data correction, while one dashboard integration assertion still expects 151.

## Environment and deployment touchpoints

- local connection configuration: ASP.NET Core User Secrets;
- local launch profiles: `src/OcpProduction.Web/Properties/launchSettings.json`;
- no publish profile, certificate, container manifest, cloud deployment manifest, or CI/CD workflow was found;
- no background job or hosted-service implementation was identified;
- deployment architecture and operational secret management remain unknown.

## Non-application workspace areas

| Area | Contents | Publication note |
|---|---|---|
| `docs/` | Functional and technical documents, PDFs, and assumptions | Review binary documents for confidentiality |
| `Research/` | External research PDFs and one DOCX | Verify provenance, licensing, and confidential content |
| `backups/` | Historical script/document copies | Preserve until manual comparison is complete |
| `Power BI/` | Currently no PBIX | Add only an approved report; imported data may be sensitive |
| root schema SQL | Standalone schema export | Compare with canonical `sql/` scripts before publication |

## Highest-leverage improvement opportunities

1. Resolve the hard-coded demo authentication secret before any publication.
2. Update the authorized TruckStop correction integration baseline and restore a green suite.
3. Decide whether to preserve the nested repository or perform a documented parent-history migration.
4. Add a CI workflow for restore, build, database-independent tests, and secret scanning.
5. Review all PDFs, DOCX files, and future PBIX content for confidentiality and licensing.
6. Compare historical backup copies by hash and purpose before deciding retention.
7. Establish a canonical database creation and reporting-script order in one database README.
8. Add an approved synthetic-data statement and review process for repository exports.
9. Document a non-demo authentication strategy before any deployment work.
10. Add a release checklist covering clean Git state, secret scan, binary review, tests, and backup exclusion.

