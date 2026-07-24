using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;

namespace OcpProduction.Application.Trips.Services;

public interface ITripWriteService
{
    Task<TripWriteResult> CreateAsync(
        CreateTripCommand command,
        CancellationToken cancellationToken = default);

    Task<TripEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<TripWriteResult> UpdateAsync(
        UpdateTripCommand command,
        CancellationToken cancellationToken = default);

    Task<TripFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default);
}
