using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public sealed class DriverPerformanceDetailsViewModel
{
    public DriverPerformanceDetailsDto Performance { get; init; } = new();
    public DriverPerformanceSearchCriteria ReturnCriteria { get; init; } = new();
}
