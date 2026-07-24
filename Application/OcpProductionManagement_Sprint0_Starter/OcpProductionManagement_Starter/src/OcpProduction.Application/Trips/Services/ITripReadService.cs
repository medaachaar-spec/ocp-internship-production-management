using OcpProduction.Application.Trips.Dtos;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Trips.Services;

public interface ITripReadService
{
    Task<PagedResult<TripListItemDto>> SearchAsync(
        TripSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<TripDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<TripFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
