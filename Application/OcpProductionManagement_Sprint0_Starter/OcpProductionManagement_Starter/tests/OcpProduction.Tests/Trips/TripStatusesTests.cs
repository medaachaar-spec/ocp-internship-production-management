using OcpProduction.Application.Trips;
using OcpProduction.Web.ViewModels.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripStatusesTests
{
    [Fact]
    public void Technical_values_and_French_labels_are_centralized()
    {
        Assert.Equal("InProgress", TripStatuses.InProgress);
        Assert.Equal("PendingValidation", TripStatuses.PendingValidation);
        Assert.Equal("Completed", TripStatuses.Completed);
        Assert.Equal("En cours", TripPresentation.GetStatusLabel(TripStatuses.InProgress));
        Assert.Equal("À valider", TripPresentation.GetStatusLabel(TripStatuses.PendingValidation));
        Assert.Equal("Validé", TripPresentation.GetStatusLabel(TripStatuses.Completed));
    }

    [Theory]
    [InlineData("InProgress", "PendingValidation", true)]
    [InlineData("PendingValidation", "Completed", true)]
    [InlineData("InProgress", "Completed", false)]
    [InlineData("Completed", "InProgress", false)]
    [InlineData("Completed", "PendingValidation", false)]
    [InlineData("PendingValidation", "InProgress", false)]
    [InlineData("Unknown", "Completed", false)]
    public void Only_the_two_documented_transitions_are_allowed(
        string currentStatus,
        string nextStatus,
        bool expected)
    {
        Assert.Equal(expected, TripStatuses.CanTransition(currentStatus, nextStatus));
    }

    [Fact]
    public void Unknown_status_uses_neutral_presentation()
    {
        Assert.Equal("Unknown", TripPresentation.GetStatusLabel("Unknown"));
        Assert.Equal("text-bg-secondary", TripPresentation.GetStatusBadgeClass("Unknown"));
    }
}
