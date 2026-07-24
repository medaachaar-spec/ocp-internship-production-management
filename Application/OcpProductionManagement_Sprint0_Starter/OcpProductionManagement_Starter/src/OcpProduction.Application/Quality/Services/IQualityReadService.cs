using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Quality.Services;

public interface IQualityReadService
{
    Task<PagedResult<QualityResultListItemDto>> SearchAsync(
        QualitySearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<QualityResultDetailsDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<QualityFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
