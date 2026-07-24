using OcpProduction.Application.Trucks.Dtos;
using OcpProduction.Application.Trucks.Models;

namespace OcpProduction.Web.ViewModels.Trucks;

public sealed class TruckDetailsViewModel
{
    public required TruckDetailsDto Truck { get; init; }
    public required TruckSearchCriteria ReturnCriteria { get; init; }
    public bool CanViewTrips { get; init; }
    public bool CanViewStops { get; init; }

    public Dictionary<string, string?> ReturnRouteValues => TruckNavigation.ForList(ReturnCriteria);
}
