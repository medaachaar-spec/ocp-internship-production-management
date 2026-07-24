using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Models;

namespace OcpProduction.Application.Stops.Services;

public interface IStopWriteService
{
    Task<StopWriteResult> CreateAsync(
        CreateStopCommand command,
        CancellationToken cancellationToken = default);

    Task<StopEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<StopWriteResult> UpdateAsync(
        UpdateStopCommand command,
        CancellationToken cancellationToken = default);

    Task<StopFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default);
}
