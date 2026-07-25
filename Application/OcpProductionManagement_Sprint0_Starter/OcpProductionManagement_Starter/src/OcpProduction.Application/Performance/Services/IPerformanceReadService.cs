using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Performance.Services;

public interface IPerformanceReadService
{
    Task<TruckPerformanceReportDto> GetTruckReportAsync(
        TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TruckPerformanceListItemDto>> SearchTrucksAsync(
        TruckPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<TruckPerformanceDetailsDto?> GetTruckAsync(
        int truckId,
        CancellationToken cancellationToken = default);

    Task<DriverPerformanceReportDto> GetDriverReportAsync(
        DriverPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DriverPerformanceListItemDto>> SearchDriversAsync(
        DriverPerformanceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<DriverPerformanceDetailsDto?> GetDriverAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<PerformanceFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
