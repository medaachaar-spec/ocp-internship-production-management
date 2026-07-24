# Power BI reporting

## Current file status

No `.pbix` file was present in this folder during the workspace audit on 2026-07-24. Add the approved report here only after confirming that its imported data and metadata are acceptable for the private repository.

## Expected source

- SQL Server database: `OCP_Mining_Prototype`
- Authentication: configure interactively or through an approved managed data source
- Storage mode: Import unless a separately validated requirement calls for DirectQuery

Do not store credentials, passwords, gateway secrets, or private connection details in repository documentation.

## Required reporting views

The current application and validated reporting model reference:

- `dbo.vw_TripSummary`;
- `dbo.vw_StopAnalysis`;
- `dbo.vw_QualityAnalysis`;
- `dbo.vw_TruckPerformance`;
- `dbo.vw_DriverPerformance`;
- `dbo.vw_TripsDailySummary`;
- `dbo.vw_StopsByReason`;
- `dbo.vw_ExecutiveDashboard`;
- `dbo.vw_GlobalProductionSummary`.

Verify that the view definitions in the database match the reviewed SQL scripts before refreshing.

## Date model

Use a dedicated `Calendrier` table with active, one-to-many, single-direction relationships:

- `Calendrier[Date]` → `vw_TripSummary[TripDate]`;
- `Calendrier[Date]` → `vw_StopAnalysis[StopDate]`;
- `Calendrier[Date]` → `vw_QualityAnalysis[QualityDate]`.

The detailed views are authoritative for date-filtered KPI calculations. Global aggregate views must not be reused as if they were date-grained facts.

## Major report pages

The validated reporting scope supports:

- executive overview;
- trips;
- truck stops;
- quality analyses;
- truck activity and performance facts;
- driver activity and performance facts.

Page names and layout should be verified against the actual PBIX when it becomes available.

## Refresh process

1. Open the approved PBIX in Power BI Desktop.
2. Open **Data source settings** and select the intended local or managed SQL Server source.
3. Authenticate through the Power BI prompt; do not paste credentials into documentation.
4. Confirm the database is `OCP_Mining_Prototype`.
5. Refresh the model.
6. Review refresh errors, relationship status, row counts, and the validated KPI baselines.
7. Save only after confirming that imported data is approved for repository storage.

When transferring the report to another user or environment, clear or replace local data-source permissions through Power BI rather than distributing credentials.

## Modelling rules

- preserve the validated grain of each detailed view;
- use distinct counts for unique business identifiers where defined;
- keep linked and unlinked records when the validated KPI includes both;
- do not assume identifiers are sequential;
- do not replace exact stored status values with inferred labels;
- do not invent targets, rankings, conformity rules, or performance scores;
- do not add a P2O5 target or conformity threshold without an approved business definition;
- keep authorized data corrections traceable and update baselines explicitly.

## Confidentiality and Git

A PBIX is a binary file and may contain imported rows, model metadata, source names, and cached results. Treat it as potentially sensitive even in a private repository.

- review the report and its data before committing;
- never commit Power BI lock or temporary files;
- retain the main approved PBIX unless policy requires external storage;
- consider Git LFS if the PBIX grows materially, while noting that LFS does not reduce confidentiality risk;
- do not configure Git LFS until the repository strategy is approved.

