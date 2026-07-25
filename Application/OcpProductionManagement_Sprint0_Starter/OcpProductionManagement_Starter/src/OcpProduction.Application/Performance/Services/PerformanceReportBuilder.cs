using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Performance.Services;

public static class PerformanceReportBuilder
{
    public static TruckPerformanceReportDto BuildTruckReport(
        IReadOnlyList<TruckPerformanceListItemDto> filteredItems,
        TruckPerformanceSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(filteredItems);
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        var tableItems = ApplyTruckSort(filteredItems, normalized.Sort).ToList();
        var page = CreatePage(tableItems, normalized.Page, normalized.PageSize);

        return new TruckPerformanceReportDto
        {
            FilteredItems = filteredItems,
            TableRows = page,
            Summary = new TruckPerformanceSummaryDto
            {
                DisplayedTruckCount = filteredItems
                    .Select(item => item.TruckId)
                    .Distinct()
                    .Count(),
                TripCount = filteredItems.Sum(item => item.TotalTrips),
                CompletedTonnage = filteredItems.Sum(item => item.CompletedTonnage),
                StopCount = filteredItems.Sum(item => item.TotalStopCount ?? 0),
                TotalStopDurationMinutes = filteredItems.Sum(
                    item => item.TotalDowntimeMinutes ?? 0)
            },
            Charts = new TruckPerformanceChartDataDto
            {
                TripsByTruck = filteredItems
                    .OrderByDescending(item => item.TotalTrips)
                    .ThenBy(item => item.TruckCode, StringComparer.Ordinal)
                    .ThenBy(item => item.TruckId)
                    .Select(item => new CategoryLongValueDto
                    {
                        EntityId = item.TruckId,
                        Category = item.TruckCode,
                        Value = item.TotalTrips
                    })
                    .ToList(),
                TonnageByTruck = filteredItems
                    .OrderByDescending(item => item.CompletedTonnage)
                    .ThenBy(item => item.TruckCode, StringComparer.Ordinal)
                    .ThenBy(item => item.TruckId)
                    .Select(item => new CategoryDecimalValueDto
                    {
                        EntityId = item.TruckId,
                        Category = item.TruckCode,
                        Value = item.CompletedTonnage
                    })
                    .ToList(),
                StopsByTruck = filteredItems
                    .OrderByDescending(item => item.TotalStopCount ?? 0)
                    .ThenBy(item => item.TruckCode, StringComparer.Ordinal)
                    .ThenBy(item => item.TruckId)
                    .Select(item => new CategoryLongValueDto
                    {
                        EntityId = item.TruckId,
                        Category = item.TruckCode,
                        Value = item.TotalStopCount ?? 0
                    })
                    .ToList(),
                P2O5ByTruck = filteredItems
                    .Where(item => item.AverageP2O5.HasValue)
                    .OrderByDescending(item => item.AverageP2O5)
                    .ThenBy(item => item.TruckCode, StringComparer.Ordinal)
                    .ThenBy(item => item.TruckId)
                    .Select(item => new CategoryDecimalValueDto
                    {
                        EntityId = item.TruckId,
                        Category = item.TruckCode,
                        Value = item.AverageP2O5!.Value
                    })
                    .ToList()
            }
        };
    }

    public static DriverPerformanceReportDto BuildDriverReport(
        IReadOnlyList<DriverPerformanceListItemDto> filteredItems,
        DriverPerformanceSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(filteredItems);
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        var tableItems = ApplyDriverSort(filteredItems, normalized.Sort).ToList();
        var page = CreatePage(tableItems, normalized.Page, normalized.PageSize);

        return new DriverPerformanceReportDto
        {
            FilteredItems = filteredItems,
            TableRows = page,
            Summary = new DriverPerformanceSummaryDto
            {
                DisplayedDriverCount = filteredItems
                    .Select(item => item.DriverId)
                    .Distinct()
                    .Count(),
                TripCount = filteredItems.Sum(item => item.TotalTrips),
                CompletedTripCount = filteredItems.Sum(item => item.CompletedTrips),
                CompletedTonnage = filteredItems.Sum(item => item.CompletedTonnage),
                LinkedStopCount = filteredItems.Sum(
                    item => item.TotalLinkedStopCount ?? 0)
            },
            Charts = new DriverPerformanceChartDataDto
            {
                TripsByDriver = filteredItems
                    .OrderByDescending(item => item.TotalTrips)
                    .ThenBy(item => item.DriverCode, StringComparer.Ordinal)
                    .ThenBy(item => item.DriverId)
                    .Select(item => new DriverTripChartPointDto
                    {
                        DriverId = item.DriverId,
                        Category = $"{item.DriverFullName} ({item.DriverCode})",
                        TotalTrips = item.TotalTrips,
                        CompletedTrips = item.CompletedTrips
                    })
                    .ToList(),
                TonnageByDriver = filteredItems
                    .OrderByDescending(item => item.CompletedTonnage)
                    .ThenBy(item => item.DriverCode, StringComparer.Ordinal)
                    .ThenBy(item => item.DriverId)
                    .Select(item => new CategoryDecimalValueDto
                    {
                        EntityId = item.DriverId,
                        Category = $"{item.DriverFullName} ({item.DriverCode})",
                        Value = item.CompletedTonnage
                    })
                    .ToList(),
                LinkedStopsByDriver = filteredItems
                    .OrderByDescending(item => item.TotalLinkedStopCount ?? 0)
                    .ThenBy(item => item.DriverCode, StringComparer.Ordinal)
                    .ThenBy(item => item.DriverId)
                    .Select(item => new CategoryLongValueDto
                    {
                        EntityId = item.DriverId,
                        Category = $"{item.DriverFullName} ({item.DriverCode})",
                        Value = item.TotalLinkedStopCount ?? 0
                    })
                    .ToList()
            }
        };
    }

    private static IEnumerable<TruckPerformanceListItemDto> ApplyTruckSort(
        IEnumerable<TruckPerformanceListItemDto> items,
        string sort)
    {
        return sort switch
        {
            TruckPerformanceSort.TotalTonnageDesc => items
                .OrderByDescending(item => item.CompletedTonnage)
                .ThenBy(item => item.TruckCode, StringComparer.Ordinal),
            TruckPerformanceSort.DowntimeDesc => items
                .OrderByDescending(item => item.TotalDowntimeMinutes ?? 0)
                .ThenBy(item => item.TruckCode, StringComparer.Ordinal),
            TruckPerformanceSort.CompletionRateDesc => items
                .OrderByDescending(item => item.CompletionRatePercent ?? 0)
                .ThenBy(item => item.TruckCode, StringComparer.Ordinal),
            TruckPerformanceSort.CodeAsc => items
                .OrderBy(item => item.TruckCode, StringComparer.Ordinal),
            _ => items
                .OrderByDescending(item => item.TotalTrips)
                .ThenBy(item => item.TruckCode, StringComparer.Ordinal)
        };
    }

    private static IEnumerable<DriverPerformanceListItemDto> ApplyDriverSort(
        IEnumerable<DriverPerformanceListItemDto> items,
        string sort)
    {
        return sort switch
        {
            DriverPerformanceSort.TotalTonnageDesc => items
                .OrderByDescending(item => item.CompletedTonnage)
                .ThenBy(item => item.DriverCode, StringComparer.Ordinal),
            DriverPerformanceSort.StopDurationDesc => items
                .OrderByDescending(item => item.TotalLinkedStopDurationMinutes ?? 0)
                .ThenBy(item => item.DriverCode, StringComparer.Ordinal),
            DriverPerformanceSort.CompletionRateDesc => items
                .OrderByDescending(item => item.CompletionRatePercent ?? 0)
                .ThenBy(item => item.DriverCode, StringComparer.Ordinal),
            DriverPerformanceSort.CodeAsc => items
                .OrderBy(item => item.DriverCode, StringComparer.Ordinal),
            _ => items
                .OrderByDescending(item => item.TotalTrips)
                .ThenBy(item => item.DriverCode, StringComparer.Ordinal)
        };
    }

    private static PagedResult<T> CreatePage<T>(
        IReadOnlyList<T> orderedItems,
        int requestedPage,
        int pageSize)
    {
        var totalItems = orderedItems.Count;
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);
        var page = Math.Clamp(requestedPage, 1, Math.Max(totalPages, 1));
        var items = orderedItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PagedResult<T>.Create(items, page, pageSize, totalItems);
    }
}
