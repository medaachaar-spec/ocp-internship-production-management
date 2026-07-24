# Data Assumptions

## Nature des données

Les données opérationnelles de cette base sont entièrement synthétiques. Elles sont inspirées de références publiques relatives à Phosboucraa et de benchmarks issus de mines à ciel ouvert comparables. Elles ne constituent ni des données internes, ni des chiffres opérationnels officiels d'OCP.

## 1. Publicly Confirmed Phosboucraa Facts

These facts are used only as public context:

| Fact | How the Prototype Uses It | Source |
|---|---|---|
| Boucraa is an OCP mining site in the Southern Provinces. | The neutral loading points use the general site context `Mine de Boucraa`. | [OCP Group - Industrial operations](https://www.ocpgroup.ma/en/what-we-do/industrial-operations) |
| Public sources describe phosphate transport from Boucraa to Laayoune by the world's longest conveyor belt, around 102 km. | Trips are interpreted as internal mine movements, not road transport over the complete Boucraa-Laayoune distance. | [OCP Group - Industrial operations](https://www.ocpgroup.ma/en/what-we-do/industrial-operations), [NASA Earth Observatory](https://science.nasa.gov/earth/earth-observatory/worlds-longest-conveyor-belt-system-150869/) |

No internal OCP zone names, fleet identifiers, employee identities, production volumes, quality thresholds, shift schedules, stop taxonomies, or truck models are used.

## 2. Mining-Industry Benchmarks

These benchmarks inform the fictional ranges but are not copied as OCP rules.

| Topic | Benchmark Used | Prototype Choice |
|---|---|---|
| Open-pit haulage cycle composition | Public mining literature describes truck cycle time as loading, loaded travel, dumping, waiting/spotting, and empty return. | The prototype stores only total trip duration and documents decomposition as a future improvement. |
| Open-pit cycle-time magnitude | A public dump-truck cycle-time study reports cycle-time components and total cycle-time analysis for an open-pit context. | Normal internal movements are generated at 18-30 minutes, with analytical long trips at 31-50 minutes. |
| Loading time | A public loading/hauling optimization study reports loading times around a few minutes. | No decomposition fields are added; this supports future decomposition ranges. |
| P2O5 and BPL relationship | Public phosphate references report BPL/P2O5 conversion relationships. | BPL is documented as a future derived metric; the MVP stores only P2O5. |
| Phosphate quality grade context | Public phosphate-rock references commonly discuss P2O5 percentage and BPL grades. | P2O5 is generated between 28.5% and 31.5% for demonstration only. |

Useful benchmark references:

- [Cycle Time Analysis of Open Pit Mining Dump Trucks](https://www.scirp.org/journal/paperinformation?paperid=127023)
- [Optimization of cycle time for loading and hauling trucks in open-pit mining](https://mining.in.ua/articles/volume18_1/03.pdf)
- [USGS phosphate rock publication, including BPL/P2O5 conversion notes](https://d9-wret.s3.us-west-2.amazonaws.com/assets/palladium/production/mineral-pubs/phosphate-rock/540495.pdf)
- [Beneficiation of Idaho Phosphate Rock, including P2O5 to BPL conversion context](https://www.idahogeology.org/pub/Mineral_Resource_Reports/MR-03.pdf)

## 3. Prototype Engineering Assumptions

### Trips

- 800 trips over 30 days are a partial analytical sample for dashboard demonstration.
- Daily trip counts vary from 23 to 31.
- Trips represent internal mine haulage movements.
- All trucks, drivers, shifts, loading points, and destinations appear in the sample.
- Completed trip payload is between 50 t and 90 t.
- Payload is compatible with the fictional rated payload of the assigned truck.
- Normal completed trip duration is 18-30 minutes.
- Long completed trips are 31-50 minutes and represent analytical long trips, not official anomalies.
- About 2% of trips are incomplete or in progress.

### Truck Stops

- 150 truck stops are generated.
- Mechanical stops are less frequent but longer.
- Operational waiting stops are more frequent but shorter.
- Preventive maintenance is the only planned stop reason.
- Stop reasons are benchmark-based prototype categories, not an official OCP taxonomy.

### Quality

- 120 `QualityResult` rows represent 120 P2O5 analyses.
- P2O5 values are between 28.5% and 31.5%.
- `TargetValue` and `Status` are prototype review helpers only and are not official OCP thresholds.
- BPL, moisture, SiO2/insolubles, and optional CO2 are future extensions.

## 4. Items Requiring Supervisor Validation

- Real loading points and zones.
- Real internal destinations.
- Real truck identifiers and equipment classes.
- Real driver identifiers and anonymization rules.
- Real shift organization.
- Real stop-reason taxonomy.
- Real trip definition and timestamps.
- Real payload source field.
- Real quality metrics, units, laboratory batch structure, and thresholds.
- Official KPI definitions for availability, utilization, mechanical availability, MTBF, MTTR, and payload compliance.

