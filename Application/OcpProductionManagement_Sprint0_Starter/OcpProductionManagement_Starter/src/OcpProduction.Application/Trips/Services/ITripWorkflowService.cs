using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Trips.Services;

public interface ITripWorkflowService
{
    Task<TripClosureData?> GetForClosureAsync(
        long tripId,
        CancellationToken cancellationToken = default);

    Task<TripWorkflowResult> CloseAsync(
        CloseTripCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PendingTripListItemDto>> GetPendingValidationAsync(
        TripValidationSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<long> GetPendingValidationCountAsync(
        CancellationToken cancellationToken = default);

    Task<TripWorkflowResult> ValidateAsync(
        long tripId,
        CancellationToken cancellationToken = default);
}
