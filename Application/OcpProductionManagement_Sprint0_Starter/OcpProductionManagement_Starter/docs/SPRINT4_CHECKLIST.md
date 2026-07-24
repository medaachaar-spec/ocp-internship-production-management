# Sprint 4 Checklist

- [x] Baseline repository verified on `main`.
- [x] Frozen SQL Server compatibility inspected with read-only metadata and count/fingerprint queries.
- [x] Trucks module implemented as read-only list/details with server-side search, filters, pagination, and role-aware navigation.
- [x] Drivers module implemented as read-only list/details with server-side search, filters, pagination, and role-aware navigation.
- [x] Truck performance view implemented from `vw_TruckPerformance`.
- [x] Driver performance view implemented from `vw_DriverPerformance`.
- [x] Performance access uses existing `CanViewReporting` policy.
- [x] Administrative reference-data catalog implemented with `CanAdminister`.
- [x] Reference-data writes limited to compatible tables only: WorkShift, LoadingPoint, Destination, StopReason.
- [x] Create, unused-row edit, and activate/deactivate paths implemented.
- [x] Delete, physical removal, schema changes, migrations, Truck/Driver/Trip/TruckStop/QualityResult writes, and generated-model edits not implemented.
- [x] All write actions are POST-only and antiforgery-protected.
- [x] Rollback-based SQL Server integration tests passed.
- [x] Final restore, formatting, build, tests, vulnerable-package scan, secret scan, migration scan, database safety check, and runtime smoke test passed.
- [x] Sprint 4 checkpoint commits created locally.
