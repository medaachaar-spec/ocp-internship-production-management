using OcpProduction.Application.Quality.Commands;
using OcpProduction.Application.Quality.Models;

namespace OcpProduction.Application.Quality.Services;

public interface IQualityWriteService
{
    Task<QualityWriteResult> CreateAsync(
        CreateQualityResultCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> UpdateAsync(
        UpdateQualityResultCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default);
}
