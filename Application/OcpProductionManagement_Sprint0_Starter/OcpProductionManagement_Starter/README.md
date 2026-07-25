# OCP Production Management

Presentation-ready application foundation for the OCP Phosboucraa
mining-production management internship project.

## Current application scope

The current application includes role-aware Home content, operational Trips and
Stops workflows, Quality entry and review, supervisor validation, read-only
Truck and Driver catalogs, controlled reference-data administration, a
six-KPI/five-chart production Dashboard, factual Truck and Driver Performance
pages, and an administrator-only Technical Diagnostic.

The database schema and reporting definitions are frozen. The application does
not create migrations at startup, reporting services remain read-only, and no
quality threshold, performance score, or ranking is inferred.

The sections below preserve the implementation history; they do not describe an
unfinished application.

## Sprint 0 status

Sprint 0 is closed. The Database-First model integrates the frozen SQL Server
database, cookie authentication, role authorization, diagnostics, and automated
verification without creating migrations.

## Sprint 1 status

The Trips module provides real SQL-backed lists, filters, pagination, details,
controlled creation and editing, closure, and supervisor validation. Workflow
tests use transactions that are rolled back.

## Sprint 2 status

The Truck Stops module provides lists, details, filters, controlled creation and
editing, closure, and validation. Invalid or forged transitions are rejected
server-side.

## Sprint 3 status

The Quality module provides list, detail, filter, creation, and correction
flows. No P2O5 conformity threshold or unsupported validation lifecycle is
invented.

## Sprint 4 status

Trucks and Drivers are available through read-only catalog and factual
performance views. Compatible reference-data management is limited to
controlled create, edit, activation, and deactivation operations.

## Sprint 5 status

The integrated dashboard uses read-only reporting views, inclusive date
filtering, six KPI cards, and five accessible Chart.js visualizations. Dashboard
access uses its dedicated authorization policy.

## Technical scope

- ASP.NET Core MVC on .NET 10;
- Entity Framework Core Database-First;
- SQL Server;
- cookie authentication;
- five prototype roles;
- policy-based authorization;
- role-aware sidebar;
- Trips, Stops, Quality, Trucks, Drivers, and reference-data modules;
- read-only operational dashboard and performance reporting;
- administrator-only runtime diagnostics;
- xUnit automated tests.

## Prerequisites

- Windows 10 or 11;
- .NET 10 SDK;
- SQL Server and SSMS;
- Git;
- optional Visual Studio or VS Code.

## 1. Configure SQL Server locally

The Web project already uses ASP.NET Core User Secrets. Configure the local
database connection from the repository root:

```powershell
dotnet user-secrets set "ConnectionStrings:OcpDatabase" "<local-sql-connection-string>" --project src/OcpProduction.Web
```

Never commit a real connection string.

## 2. Configure local demo authentication

Demo authentication is intended only for this internship prototype. Usernames
and passwords must be supplied through ASP.NET Core User Secrets during local
development, or through environment variables in another explicitly approved
environment.

The following commands contain placeholders only. Replace each placeholder
locally and do not reuse a personal or production password.

### Superviseur

```powershell
dotnet user-secrets set "Authentication:DemoUsers:0:Username" "<local-superviseur-username>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:0:Password" "<local-superviseur-password>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:0:Role" "Superviseur" --project src/OcpProduction.Web
```

### ResponsableProduction

```powershell
dotnet user-secrets set "Authentication:DemoUsers:1:Username" "<local-responsable-production-username>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:1:Password" "<local-responsable-production-password>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:1:Role" "ResponsableProduction" --project src/OcpProduction.Web
```

### Administrateur

```powershell
dotnet user-secrets set "Authentication:DemoUsers:2:Username" "<local-administrateur-username>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:2:Password" "<local-administrateur-password>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:2:Role" "Administrateur" --project src/OcpProduction.Web
```

### AgentProduction

```powershell
dotnet user-secrets set "Authentication:DemoUsers:3:Username" "<local-agent-production-username>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:3:Password" "<local-agent-production-password>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:3:Role" "AgentProduction" --project src/OcpProduction.Web
```

### AgentQualite

```powershell
dotnet user-secrets set "Authentication:DemoUsers:4:Username" "<local-agent-qualite-username>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:4:Password" "<local-agent-qualite-password>" --project src/OcpProduction.Web
dotnet user-secrets set "Authentication:DemoUsers:4:Role" "AgentQualite" --project src/OcpProduction.Web
```

User Secrets are stored outside the repository and are not committed by Git.
Missing or invalid enabled-demo configuration causes authentication startup
validation to fail without creating a fallback account.

For environment variables, use the standard ASP.NET Core double-underscore
notation, such as `Authentication__DemoUsers__0__Username`. Do not place real
values in tracked scripts or launch profiles.

Production authentication requires a separate, reviewed identity solution.
The demo provider must not be treated as a production identity system.

## 3. Restore, build, and run

```powershell
dotnet restore
dotnet build OcpProductionManagement.sln
dotnet run --project src/OcpProduction.Web
```

Open the local address printed by ASP.NET Core.

## 4. Run automated tests

```powershell
dotnet build OcpProductionManagement.sln --no-restore --configuration Debug
dotnet test OcpProductionManagement.sln --no-build --configuration Debug
```

SQL integration tests require the compatible local
`OCP_Mining_Prototype` database.

## 5. Database-First model

Scaffolding is complete. The retained scaffolding script is for deliberate,
reviewed regeneration only:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/scaffold-database.ps1
```

Do not run it during ordinary application startup. The SQL schema is frozen:
do not add migrations and do not edit generated entities directly.

## Security rules

- keep credentials in User Secrets or an approved environment secret store;
- never log submitted or configured passwords;
- never commit `.env`, `secrets.json`, certificates, or private keys;
- rotate local demo credentials if they may have been disclosed;
- review Git history before the first remote push;
- disable demo authentication before introducing a real identity provider.
