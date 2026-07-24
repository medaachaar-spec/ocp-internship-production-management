using OcpProduction.Application.Drivers.Models;
using OcpProduction.Application.Trucks.Models;
using OcpProduction.Web.ViewModels.Performance;
using Xunit;

namespace OcpProduction.Tests.Performance;

public sealed class PerformancePresentationTests
{
    [Fact]
    public void Performance_presentation_uses_factual_status_labels_and_fallbacks()
    {
        Assert.Equal("En service", PerformancePresentation.GetTruckStatusLabel(TruckStatusDefinitions.InService));
        Assert.Equal("En congé", PerformancePresentation.GetDriverStatusLabel(DriverStatusDefinitions.OnLeave));
        Assert.Equal("—", PerformancePresentation.OrDash(" "));
        Assert.Equal("—", PerformancePresentation.FormatDecimal(null));
        Assert.Equal("12,50 %", PerformancePresentation.FormatPercent(12.5m));
        Assert.DoesNotContain("meilleur", PerformancePresentation.GetDriverStatusLabel(DriverStatusDefinitions.Active), StringComparison.OrdinalIgnoreCase);
    }
}
