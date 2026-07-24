using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Web.ViewModels.Drivers;
using OcpProduction.Web.ViewModels.Trucks;
using Xunit;

namespace OcpProduction.Tests.Fleet;

public sealed class FleetPresentationTests
{
    [Fact]
    public void Truck_presentation_uses_actual_statuses_and_neutral_fallbacks()
    {
        Assert.Equal("En service", TruckPresentation.GetStatusLabel(TruckStatusDefinitions.InService));
        Assert.Equal("Unexpected", TruckPresentation.GetStatusLabel("Unexpected"));
        Assert.Equal("—", TruckPresentation.OrFallback(" "));
        Assert.Equal("1 h 15 min", TruckPresentation.FormatDuration(75L));
        Assert.DoesNotContain("meilleur", TruckPresentation.GetStatusLabel(TruckStatusDefinitions.Available), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Driver_presentation_is_factual_and_handles_nulls()
    {
        Assert.Equal("En congé", DriverPresentation.GetStatusLabel(DriverStatusDefinitions.OnLeave));
        Assert.Equal("Unexpected", DriverPresentation.GetStatusLabel("Unexpected"));
        Assert.Equal("—", DriverPresentation.FormatDuration((long?)null));
        Assert.Equal("30 min", DriverPresentation.FormatDuration(30L));
        Assert.DoesNotContain("mauvais", DriverPresentation.GetStatusLabel(DriverStatusDefinitions.Inactive), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Latest_activity_selects_the_most_recent_record()
    {
        var earlier = new DateTime(2026, 6, 1, 8, 0, 0);
        var later = earlier.AddHours(2);
        Assert.Equal(later, TruckPresentation.LatestActivity(earlier, later));
        Assert.Equal(later, DriverPresentation.LatestActivity(later, earlier));
    }
}
