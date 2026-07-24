# Sprint 5 dashboard current baselines

These are the active manual-verification baselines after the authorized deletion
of `dbo.TruckStop.TruckStopID = 153`. They describe the current corrected
database, not the historical state recorded by earlier sprint reports.

## Unfiltered dashboard

| KPI | Expected value |
|---|---:|
| Nombre de voyages | 801 |
| Tonnage total | 54,533.80 t |
| Nombre d'arrêts | 150 |
| Durée totale des arrêts | 416.40 h |
| Nombre d'analyses qualité | 120 |
| Voyages en attente de validation | 0 |

The 150 remaining stops represent 24,984 minutes: 120 are linked to a Trip and
30 are not linked to a Trip. `TruckStopID 153` must be absent.

## June 2026 dashboard

Use the inclusive period from `2026-06-01` through `2026-06-30`.

| KPI | Expected value |
|---|---:|
| Nombre de voyages | 800 |
| Tonnage total | 54,473.80 t |
| Nombre d'arrêts | 149 |
| Durée totale des arrêts | 415.50 h |
| Nombre d'analyses qualité | 120 |
| Voyages en attente de validation | 0 |

The June stop duration remains 24,930 minutes.

## July correction check

For the inclusive period from `2026-07-01` through `2026-07-20`, the corrected
stop baseline is one stop totaling 54 minutes.

## Presentation checks

- The stop-duration chart remains sourced from the current reporting data.
- The chart must include all remaining linked and unlinked stops.
- No hard-coded exceptional-stop explanation is displayed.
- The Home page is outside the dashboard change and remains unchanged.

