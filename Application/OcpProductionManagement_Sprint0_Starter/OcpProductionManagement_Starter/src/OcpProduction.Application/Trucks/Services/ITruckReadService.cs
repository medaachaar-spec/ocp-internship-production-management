using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trucks.Dtos;
using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Application.Trucks.Services;

public interface ITruckReadService
{
    Task<PagedResult<TruckListItemDto>> SearchAsync(
        TruckSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<TruckDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TruckFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
