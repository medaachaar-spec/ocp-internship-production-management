using OcpProduction.Application.ReferenceData.Dtos;

namespace OcpProduction.Application.ReferenceData.Services;

public interface IReferenceDataReadService
{
    Task<IReadOnlyList<ReferenceCatalogItemDto>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkShiftDto>> GetWorkShiftsAsync(CancellationToken cancellationToken = default);
    Task<WorkShiftDto?> GetWorkShiftAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoadingPointDto>> GetLoadingPointsAsync(CancellationToken cancellationToken = default);
    Task<LoadingPointDto?> GetLoadingPointAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DestinationDto>> GetDestinationsAsync(CancellationToken cancellationToken = default);
    Task<DestinationDto?> GetDestinationAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StopReasonDto>> GetStopReasonsAsync(CancellationToken cancellationToken = default);
    Task<StopReasonDto?> GetStopReasonAsync(int id, CancellationToken cancellationToken = default);
}
