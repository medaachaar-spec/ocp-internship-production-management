using OcpProduction.Application.ReferenceData.Models;

namespace OcpProduction.Application.ReferenceData.Services;

public interface IReferenceDataWriteService
{
    Task<ReferenceDataWriteResult> CreateWorkShiftAsync(CreateWorkShiftCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> UpdateWorkShiftAsync(UpdateWorkShiftCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> SetWorkShiftActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> CreateLoadingPointAsync(CreateLoadingPointCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> UpdateLoadingPointAsync(UpdateLoadingPointCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> SetLoadingPointActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> CreateDestinationAsync(CreateDestinationCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> UpdateDestinationAsync(UpdateDestinationCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> SetDestinationActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> CreateStopReasonAsync(CreateStopReasonCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> UpdateStopReasonAsync(UpdateStopReasonCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceDataWriteResult> SetStopReasonActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
