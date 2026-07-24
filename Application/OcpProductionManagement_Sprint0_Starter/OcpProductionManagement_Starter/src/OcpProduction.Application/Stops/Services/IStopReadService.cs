using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Stops.Services;

public interface IStopReadService
{
    Task<PagedResult<StopListItemDto>> SearchAsync(
        StopSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<StopDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<StopFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
