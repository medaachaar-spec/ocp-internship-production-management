using OcpProduction.Application.Stops.Models;
using OcpProduction.Web.ViewModels.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopPresentationTests
{
    [Theory]
    [InlineData(StopStates.Open, "Ouvert")]
    [InlineData(StopStates.Closed, "Clôturé")]
    [InlineData("Unexpected", "État inconnu")]
    public void State_labels_are_safe_and_in_French(string state, string expected)
    {
        Assert.Equal(expected, StopPresentation.GetStateLabel(state));
    }

    [Theory]
    [InlineData(null, "En cours")]
    [InlineData(0, "0 min")]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(135, "2 h 15 min")]
    public void Duration_is_formatted_for_operational_display(int? minutes, string expected)
    {
        Assert.Equal(expected, StopPresentation.FormatDuration(minutes));
    }

    [Theory]
    [InlineData(StopValidationStatuses.Pending, "En attente")]
    [InlineData(StopValidationStatuses.Validated, "Validé")]
    [InlineData(StopValidationStatuses.Rejected, "Rejeté")]
    [InlineData("FutureStatus", "FutureStatus")]
    public void Validation_status_has_known_labels_and_neutral_unknown_fallback(
        string status,
        string expected)
    {
        Assert.Equal(expected, StopPresentation.GetValidationStatusLabel(status));
    }

    [Fact]
    public void Missing_observation_uses_dash_fallback()
    {
        Assert.Equal("—", StopPresentation.FormatObservation(null));
        Assert.Equal("—", StopPresentation.FormatObservation("  "));
        Assert.Equal("Observation", StopPresentation.FormatObservation("Observation"));
    }
}
