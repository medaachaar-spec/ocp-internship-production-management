using System.ComponentModel.DataAnnotations;
using OcpProduction.Web.ViewModels.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopCloseInputModelTests
{
    [Fact]
    public void Closure_requires_a_valid_identifier_and_end_time()
    {
        var input = new StopCloseInputModel();
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(StopCloseInputModel.StopId)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(StopCloseInputModel.EndTime)));
    }

    [Fact]
    public void Closure_input_does_not_expose_status_or_operational_fields()
    {
        var propertyNames = typeof(StopCloseInputModel)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            [nameof(StopCloseInputModel.EndTime), nameof(StopCloseInputModel.StopId)],
            propertyNames);
    }
}
