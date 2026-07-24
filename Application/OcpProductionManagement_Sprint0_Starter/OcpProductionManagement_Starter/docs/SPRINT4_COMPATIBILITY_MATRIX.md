# Sprint 4 Compatibility Matrix

Sprint 4 was implemented against the frozen SQL Server database `OCP_Mining_Prototype` on `localhost`. The real connection string remains in .NET User Secrets and is not documented here.

| Area | Database object | Allowed implementation | Write behavior |
|---|---|---|---|
| Trucks | `dbo.Truck` | Read-only list/details | No writes |
| Drivers | `dbo.Driver` | Read-only list/details | No writes |
| Truck performance | `dbo.vw_TruckPerformance` | Read-only all-time reporting | No writes |
| Driver performance | `dbo.vw_DriverPerformance` | Read-only all-time reporting | No writes |
| Work shifts | `dbo.WorkShift` | Admin catalog, create, edit unused rows, activate/deactivate | Rollback-tested EF writes only |
| Loading points | `dbo.LoadingPoint` | Admin catalog, create, edit unused rows, activate/deactivate | Rollback-tested EF writes only |
| Destinations | `dbo.Destination` | Admin catalog, create, edit unused rows, activate/deactivate | Rollback-tested EF writes only |
| Stop reasons | `dbo.StopReason` | Admin catalog, create, edit unused rows, activate/deactivate | Rollback-tested EF writes only |

Compatibility notes:

- No table, column, view, key, constraint, index, trigger, stored procedure, or generated EF model was changed.
- No EF migration was created.
- `Truck`, `Driver`, `Trip`, `TruckStop`, and `QualityResult` remain protected from Sprint 4 writes.
- Reference-data codes are immutable after creation.
- Existing used reference rows cannot be edited because there is no rowversion/concurrency token and because historical production records depend on them.
- Physical deletion is not implemented.
- Performance pages are all-time views because the available performance views do not expose a date dimension suitable for filtering.
