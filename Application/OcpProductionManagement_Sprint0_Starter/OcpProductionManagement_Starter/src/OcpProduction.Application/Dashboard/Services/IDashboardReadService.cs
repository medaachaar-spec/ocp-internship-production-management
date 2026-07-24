using OcpProduction.Application.Dashboard.Dtos;
using OcpProduction.Application.Dashboard.Models;

namespace OcpProduction.Application.Dashboard.Services;

public interface IDashboardReadService
{
    Task<DashboardDto> GetDashboardAsync(
        DashboardDateCriteria criteria,
        CancellationToken cancellationToken = default);
}
