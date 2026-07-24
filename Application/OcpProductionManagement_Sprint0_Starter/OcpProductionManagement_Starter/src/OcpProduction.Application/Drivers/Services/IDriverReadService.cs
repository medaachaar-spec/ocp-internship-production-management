using OcpProduction.Application.Drivers.Dtos;
using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Drivers.Services;

public interface IDriverReadService
{
    Task<PagedResult<DriverListItemDto>> SearchAsync(
        DriverSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<DriverDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<DriverFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
