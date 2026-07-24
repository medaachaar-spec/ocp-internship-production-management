using OcpProduction.Application.Performance.Dtos;
using OcpProduction.Application.Performance.Models;

namespace OcpProduction.Web.ViewModels.Performance;

public sealed class TruckPerformanceDetailsViewModel
{
    public TruckPerformanceDetailsDto Performance { get; init; } = new();
    public TruckPerformanceSearchCriteria ReturnCriteria { get; init; } = new();
}
