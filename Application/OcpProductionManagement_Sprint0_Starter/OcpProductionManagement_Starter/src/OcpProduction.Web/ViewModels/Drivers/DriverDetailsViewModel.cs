using OcpProduction.Application.Drivers.Dtos;
using OcpProduction.Application.Drivers.Models;

namespace OcpProduction.Web.ViewModels.Drivers;

public sealed class DriverDetailsViewModel
{
    public required DriverDetailsDto Driver { get; init; }
    public required DriverSearchCriteria ReturnCriteria { get; init; }
    public bool CanViewTrips { get; init; }
    public bool CanViewStops { get; init; }

    public Dictionary<string, string?> ReturnRouteValues => DriverNavigation.ForList(ReturnCriteria);
}
