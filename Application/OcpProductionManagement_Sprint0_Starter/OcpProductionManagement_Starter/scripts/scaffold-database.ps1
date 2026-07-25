$ErrorActionPreference = "Stop"

Write-Host "OCP Production - Database First scaffolding" -ForegroundColor Cyan
Write-Host "This script does not create migrations and does not update the database." -ForegroundColor Yellow

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is not installed or not available in PATH."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot

try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Local .NET tool restore failed."
    }

    $generated = "src/OcpProduction.Infrastructure/Generated"
    $context = "src/OcpProduction.Infrastructure/Data/OcpProductionDbContext.cs"

    if (Test-Path $generated) {
        Get-ChildItem $generated -Filter "*.cs" -File -ErrorAction SilentlyContinue | Remove-Item -Force
    }
    if (Test-Path $context) {
        Remove-Item $context -Force
    }

    $tables = @(
        "dbo.Truck", "dbo.Driver", "dbo.WorkShift", "dbo.LoadingPoint",
        "dbo.Destination", "dbo.StopReason", "dbo.Trip", "dbo.TruckStop",
        "dbo.QualityResult"
    )
    $views = @(
        "dbo.vw_TripSummary", "dbo.vw_TruckPerformance", "dbo.vw_DriverPerformance",
        "dbo.vw_StopAnalysis", "dbo.vw_QualityAnalysis", "dbo.vw_TripDetails",
        "dbo.vw_TripsDailySummary", "dbo.vw_TruckStopDetails", "dbo.vw_StopsByReason",
        "dbo.vw_QualityByPeriod", "dbo.vw_GlobalProductionSummary",
        "dbo.vw_ExecutiveDashboard"
    )

    $arguments = @(
        "ef", "dbcontext", "scaffold",
        "Name=ConnectionStrings:OcpDatabase",
        "Microsoft.EntityFrameworkCore.SqlServer",
        "--project", "src/OcpProduction.Infrastructure",
        "--startup-project", "src/OcpProduction.Web",
        "--context", "OcpProductionDbContext",
        "--context-dir", "Data",
        "--output-dir", "Generated",
        "--no-onconfiguring",
        "--use-database-names",
        "--force"
    )

    foreach ($databaseObject in ($tables + $views)) {
        $arguments += @("--table", $databaseObject)
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Scaffolding failed. Check the secure connection and verified SQL object names."
    }

    Write-Host "Scaffolding completed." -ForegroundColor Green
    Write-Host "Do not edit files inside Infrastructure/Generated directly." -ForegroundColor Yellow
}
finally {
    Pop-Location
}
