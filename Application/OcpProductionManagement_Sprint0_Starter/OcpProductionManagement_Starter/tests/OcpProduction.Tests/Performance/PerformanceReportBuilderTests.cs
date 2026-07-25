using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Web.ViewModels.Performance;
using System.Text.Json;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformanceReportBuilderTests
{
    [Fact]
    public void Truck_report_uses_complete_filtered_set_before_pagination_and_reconciles()
    {
        var rows = Enumerable.Range(1, 12)
            .Select(index => new TruckPerformanceListItemDto
            {
                TruckId = index,
                TruckCode = $"TRK-{index:000}",
                TruckStatus = "InService",
                TotalTrips = index,
                CompletedTrips = index - 1,
                CompletedTonnage = index * 100.25m,
                TotalStopCount = index * 2,
                TotalDowntimeMinutes = index * 15,
                AverageP2O5 = index % 2 == 0 ? 29m + index / 1000m : null
            })
            .ToList();

        var report = PerformanceReportBuilder.BuildTruckReport(
            rows,
            new TruckPerformanceSearchCriteria
            {
                Page = 1,
                PageSize = 10,
                Sort = TruckPerformanceSort.CodeAsc
            });

        Assert.Equal(12, report.Summary.DisplayedTruckCount);
        Assert.Equal(rows.Sum(row => row.TotalTrips), report.Summary.TripCount);
        Assert.Equal(rows.Sum(row => row.CompletedTonnage), report.Summary.CompletedTonnage);
        Assert.Equal(rows.Sum(row => row.TotalStopCount), report.Summary.StopCount);
        Assert.Equal(rows.Sum(row => row.TotalDowntimeMinutes), report.Summary.TotalStopDurationMinutes);
        Assert.Equal(10, report.TableRows.Items.Count);
        Assert.Equal(12, report.TableRows.TotalItems);
        Assert.Equal(12, report.FilteredItems.Count);
        Assert.Equal(12, report.Charts.TripsByTruck.Count);
        Assert.Equal(12, report.Charts.TonnageByTruck.Count);
        Assert.Equal(12, report.Charts.StopsByTruck.Count);
        Assert.Equal(6, report.Charts.P2O5ByTruck.Count);
        Assert.Equal(report.Summary.TripCount, report.Charts.TripsByTruck.Sum(point => point.Value));
        Assert.Equal(report.Summary.CompletedTonnage, report.Charts.TonnageByTruck.Sum(point => point.Value));
        Assert.Equal(report.Summary.StopCount, report.Charts.StopsByTruck.Sum(point => point.Value));
        Assert.Equal("TRK-012", report.Charts.TripsByTruck[0].Category);
        Assert.Equal("TRK-001", report.TableRows.Items[0].TruckCode);
    }

    [Fact]
    public void Driver_report_represents_all_filtered_drivers_and_linked_stops()
    {
        var rows = Enumerable.Range(1, 20)
            .Select(index => new DriverPerformanceListItemDto
            {
                DriverId = index,
                DriverCode = $"DRV-{index:000}",
                DriverFullName = $"Conducteur {index:00}",
                DriverStatus = "Active",
                TotalTrips = index + 10,
                CompletedTrips = index,
                CompletedTonnage = index * 75.50m,
                TotalLinkedStopCount = index % 4,
                TotalLinkedStopDurationMinutes = index * 12
            })
            .ToList();

        var report = PerformanceReportBuilder.BuildDriverReport(
            rows,
            new DriverPerformanceSearchCriteria
            {
                Page = 2,
                PageSize = 10,
                Sort = DriverPerformanceSort.TotalTonnageDesc
            });

        Assert.Equal(20, report.Summary.DisplayedDriverCount);
        Assert.Equal(rows.Sum(row => row.TotalTrips), report.Summary.TripCount);
        Assert.Equal(rows.Sum(row => row.CompletedTrips), report.Summary.CompletedTripCount);
        Assert.Equal(rows.Sum(row => row.CompletedTonnage), report.Summary.CompletedTonnage);
        Assert.Equal(rows.Sum(row => row.TotalLinkedStopCount), report.Summary.LinkedStopCount);
        Assert.Equal(10, report.TableRows.Items.Count);
        Assert.Equal(20, report.FilteredItems.Count);
        Assert.Equal(20, report.Charts.TripsByDriver.Count);
        Assert.Equal(20, report.Charts.TonnageByDriver.Count);
        Assert.Equal(20, report.Charts.LinkedStopsByDriver.Count);
        Assert.Equal(report.Summary.TripCount, report.Charts.TripsByDriver.Sum(point => point.TotalTrips));
        Assert.Equal(report.Summary.CompletedTripCount, report.Charts.TripsByDriver.Sum(point => point.CompletedTrips));
        Assert.Equal(report.Summary.CompletedTonnage, report.Charts.TonnageByDriver.Sum(point => point.Value));
        Assert.Equal(report.Summary.LinkedStopCount, report.Charts.LinkedStopsByDriver.Sum(point => point.Value));
        Assert.All(report.Charts.TripsByDriver, point => Assert.Contains("DRV-", point.Category, StringComparison.Ordinal));
    }

    [Fact]
    public void Empty_reports_expose_factual_zero_summaries_and_no_chart_points()
    {
        var truckReport = PerformanceReportBuilder.BuildTruckReport(
            [],
            new TruckPerformanceSearchCriteria());
        var driverReport = PerformanceReportBuilder.BuildDriverReport(
            [],
            new DriverPerformanceSearchCriteria());

        Assert.Equal(0, truckReport.Summary.DisplayedTruckCount);
        Assert.Equal(0, truckReport.Summary.TripCount);
        Assert.Equal(0m, truckReport.Summary.CompletedTonnage);
        Assert.Equal(0, truckReport.Summary.StopCount);
        Assert.Empty(truckReport.TableRows.Items);
        Assert.Empty(truckReport.Charts.TripsByTruck);
        Assert.Empty(truckReport.Charts.P2O5ByTruck);

        Assert.Equal(0, driverReport.Summary.DisplayedDriverCount);
        Assert.Equal(0, driverReport.Summary.TripCount);
        Assert.Equal(0, driverReport.Summary.CompletedTripCount);
        Assert.Equal(0m, driverReport.Summary.CompletedTonnage);
        Assert.Equal(0, driverReport.Summary.LinkedStopCount);
        Assert.Empty(driverReport.TableRows.Items);
        Assert.Empty(driverReport.Charts.TripsByDriver);
    }

    [Fact]
    public void Every_existing_table_sort_remains_deterministic()
    {
        var trucks = new[]
        {
            new TruckPerformanceListItemDto
            {
                TruckId = 2,
                TruckCode = "TRK-B",
                TotalTrips = 2,
                CompletedTonnage = 20m,
                TotalDowntimeMinutes = 30,
                CompletionRatePercent = 50m
            },
            new TruckPerformanceListItemDto
            {
                TruckId = 1,
                TruckCode = "TRK-A",
                TotalTrips = 1,
                CompletedTonnage = 10m,
                TotalDowntimeMinutes = 15,
                CompletionRatePercent = 25m
            }
        };
        var drivers = new[]
        {
            new DriverPerformanceListItemDto
            {
                DriverId = 2,
                DriverCode = "DRV-B",
                DriverFullName = "B",
                TotalTrips = 2,
                CompletedTonnage = 20m,
                TotalLinkedStopDurationMinutes = 30,
                CompletionRatePercent = 50m
            },
            new DriverPerformanceListItemDto
            {
                DriverId = 1,
                DriverCode = "DRV-A",
                DriverFullName = "A",
                TotalTrips = 1,
                CompletedTonnage = 10m,
                TotalLinkedStopDurationMinutes = 15,
                CompletionRatePercent = 25m
            }
        };

        foreach (var sort in TruckPerformanceSort.All)
        {
            var report = PerformanceReportBuilder.BuildTruckReport(
                trucks,
                new TruckPerformanceSearchCriteria { Sort = sort });
            Assert.Equal(
                sort == TruckPerformanceSort.CodeAsc ? "TRK-A" : "TRK-B",
                report.TableRows.Items[0].TruckCode);
        }

        foreach (var sort in DriverPerformanceSort.All)
        {
            var report = PerformanceReportBuilder.BuildDriverReport(
                drivers,
                new DriverPerformanceSearchCriteria { Sort = sort });
            Assert.Equal(
                sort == DriverPerformanceSort.CodeAsc ? "DRV-A" : "DRV-B",
                report.TableRows.Items[0].DriverCode);
        }
    }

    [Fact]
    public void Performance_chart_json_is_script_safe_and_round_trips_hostile_labels()
    {
        const string hostileTruckCode = "</script><script>alert('truck')</script>";
        const string hostileDriverName = "<img src=x onerror=alert('driver')>";

        var truckModel = new TruckPerformanceIndexViewModel
        {
            Report = PerformanceReportBuilder.BuildTruckReport(
                [new TruckPerformanceListItemDto
                {
                    TruckId = 1,
                    TruckCode = hostileTruckCode,
                    TruckStatus = "InService",
                    TotalTrips = 1
                }],
                new TruckPerformanceSearchCriteria())
        };
        var driverModel = new DriverPerformanceIndexViewModel
        {
            Report = PerformanceReportBuilder.BuildDriverReport(
                [new DriverPerformanceListItemDto
                {
                    DriverId = 1,
                    DriverCode = "DRV-001",
                    DriverFullName = hostileDriverName,
                    DriverStatus = "Active",
                    TotalTrips = 1
                }],
                new DriverPerformanceSearchCriteria())
        };

        Assert.DoesNotContain("</script>", truckModel.ChartDataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", driverModel.ChartDataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C", truckModel.ChartDataJson, StringComparison.Ordinal);
        Assert.Contains("\\u003C", driverModel.ChartDataJson, StringComparison.Ordinal);

        using var truckJson = JsonDocument.Parse(truckModel.ChartDataJson);
        using var driverJson = JsonDocument.Parse(driverModel.ChartDataJson);
        Assert.Equal(
            hostileTruckCode,
            truckJson.RootElement.GetProperty("tripsByTruck")[0].GetProperty("category").GetString());
        Assert.Contains(
            hostileDriverName,
            driverJson.RootElement.GetProperty("tripsByDriver")[0].GetProperty("category").GetString(),
            StringComparison.Ordinal);
    }
}
