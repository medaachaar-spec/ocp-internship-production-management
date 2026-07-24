using OcpProduction.Application.Stops.Models;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopWorkflowRulesTests
{
    [Theory]
    [InlineData(null, StopValidationStatuses.Pending, true, true, false)]
    [InlineData(null, StopValidationStatuses.Validated, false, false, false)]
    [InlineData(null, "pending", false, false, false)]
    [InlineData("2026-07-20T09:00:00", StopValidationStatuses.Pending, false, false, true)]
    [InlineData("2026-07-20T09:00:00", StopValidationStatuses.Validated, false, false, false)]
    [InlineData("2026-07-20T09:00:00", StopValidationStatuses.Rejected, false, false, false)]
    public void Only_documented_states_are_editable_closable_or_validatable(
        string? endTimeValue,
        string validationStatus,
        bool expectedEditable,
        bool expectedClose,
        bool expectedValidate)
    {
        var endTime = endTimeValue is null ? (DateTime?)null : DateTime.Parse(endTimeValue);

        Assert.Equal(expectedEditable, StopWorkflowRules.IsEditable(endTime, validationStatus));
        Assert.Equal(expectedClose, StopWorkflowRules.CanClose(endTime, validationStatus));
        Assert.Equal(expectedValidate, StopWorkflowRules.CanValidate(endTime, validationStatus));
    }

    [Fact]
    public void Existing_editability_boundary_delegates_to_workflow_rules()
    {
        Assert.True(StopEditability.IsEditable(null, StopValidationStatuses.Pending));
        Assert.False(StopEditability.IsEditable(
            new DateTime(2026, 7, 20, 9, 0, 0),
            StopValidationStatuses.Pending));
        Assert.False(StopEditability.IsEditable(null, StopValidationStatuses.Validated));
    }
}
