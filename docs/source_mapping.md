# Future Source Mapping

This file lists how the prototype fields could later be mapped to real OCP data. All mappings are tentative until confirmed with the supervisor and the real database or files.

## Mapping Table

| Prototype field | Future real OCP field | Status |
|---|---|---|
| `Truck.TruckCode` | Real truck identifier | To be mapped |
| `Truck.Model` | Real equipment type or class | To be confirmed |
| `Truck.RatedPayloadTonnes` | Real rated payload or equipment capacity | To be confirmed |
| `Driver.DriverCode` | Real driver identifier or anonymized employee key | To be mapped |
| `Driver.FullName` | Anonymized driver display name | To be defined with privacy rules |
| `WorkShift.ShiftCode` | Real shift/team/poste code | To be confirmed |
| `WorkShift.StartTime` | Real shift start time | To be confirmed |
| `WorkShift.EndTime` | Real shift end time | To be confirmed |
| `LoadingPoint.LoadingPointCode` | Real loading point or zone code | To be mapped |
| `LoadingPoint.LoadingPointName` | Real loading point or zone name | To be confirmed |
| `LoadingPoint.ZoneName` | Real site, zone, or sector grouping | To be confirmed |
| `LoadingPoint.LoadingPointDescription` | Business description or mapping note | To be confirmed |
| `Destination.DestinationCode` | Real internal destination code | To be mapped |
| `Destination.DestinationName` | Real internal destination label | To be confirmed |
| `StopReason.ReasonCode` | Real stop reason or downtime cause code | To be mapped |
| `StopReason.Category` | Real downtime category | To be confirmed |
| `Trip.TripDate` | Real operational trip date | To be confirmed |
| `Trip.ShiftID` | Real shift or production team | To be mapped |
| `Trip.TruckID` | Real truck identifier | To be mapped |
| `Trip.DriverID` | Real driver or anonymized operator key | To be mapped |
| `Trip.LoadingPointID` | Real loading point, face, or extraction area | To be mapped |
| `Trip.DestinationID` | Real crusher, stockpile, dispatch, or internal destination | To be mapped |
| `Trip.StartTime` | Real trip start timestamp | To be confirmed |
| `Trip.EndTime` | Real trip end timestamp | To be confirmed |
| `Trip.Tonnage` | Real payload, weighing, or estimated load field | To be confirmed |
| `Trip.Status` | Real trip completion status | To be mapped |
| `TruckStop.TruckID` | Real truck identifier | To be mapped |
| `TruckStop.DriverID` | Real driver or anonymized operator key, if available | Optional / to be confirmed |
| `TruckStop.ShiftID` | Real shift, if available | Optional / to be confirmed |
| `TruckStop.TripID` | Real linked trip or movement record, if available | Optional / to be confirmed |
| `TruckStop.StopReasonID` | Real downtime reason | To be mapped |
| `TruckStop.StartTime` | Real downtime start timestamp | To be confirmed |
| `TruckStop.EndTime` | Real downtime end timestamp | To be confirmed |
| `QualityResult.AnalysisDate` | Laboratory analysis timestamp | To be confirmed |
| `QualityResult.TripID` | Real sampled trip or batch linkage | Optional / to be confirmed |
| `QualityResult.LoadingPointID` | Real loading point or sample source | Optional / to be confirmed |
| `QualityResult.QualityValue` | Real P2O5 measurement in MVP scope | To be confirmed |
| `QualityResult.QualityUnit` | Real laboratory unit or metric code | To be confirmed |
| `QualityResult.TargetValue` | Real target or specification, if validated | Not official yet |
| `QualityResult.Status` | Real laboratory or quality status | To be mapped |

## Future Quality Extension

If the supervisor confirms multi-metric quality reporting, the next clean design would add:

- `dbo.QualityMetric`
- an analysis batch or group identifier
- one measurement row per metric per batch

Candidate metrics:

- P2O5
- BPL derived from P2O5
- Moisture
- SiO2 / insolubles
- Optional CO2 / carbonates

This extension is intentionally not implemented in the current MVP to preserve the simple validated count of 120 quality analyses.
