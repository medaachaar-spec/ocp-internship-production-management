using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Stops.Services;

public interface IStopWorkflowService
{
    Task<StopClosureData?> GetForClosureAsync(
        long stopId,
        CancellationToken cancellationToken = default);

    Task<StopWorkflowResult> CloseAsync(
        CloseStopCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PendingStopListItemDto>> GetPendingValidationAsync(
        StopValidationSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<long> GetPendingValidationCountAsync(
        CancellationToken cancellationToken = default);

    Task<StopWorkflowResult> ValidateAsync(
        long stopId,
        CancellationToken cancellationToken = default);
}
