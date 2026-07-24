using System.ComponentModel.DataAnnotations;
using OcpProduction.Web.ViewModels.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripCloseInputModelTests
{
    [Fact]
    public void Closure_requires_identifier_end_time_and_tonnage()
    {
        var errors = Validate(new TripCloseInputModel());

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCloseInputModel.TripId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCloseInputModel.EndTime)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCloseInputModel.Tonnage)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("100000000")]
    public void Closure_rejects_tonnage_outside_the_positive_decimal_10_2_range(string value)
    {
        var input = new TripCloseInputModel
        {
            TripId = 42,
            EndTime = new DateTime(2026, 7, 20, 9, 0, 0),
            Tonnage = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
        };

        Assert.Contains(
            Validate(input),
            error => error.MemberNames.Contains(nameof(TripCloseInputModel.Tonnage)));
    }

    [Fact]
    public void Closure_accepts_complete_positive_input()
    {
        var errors = Validate(new TripCloseInputModel
        {
            TripId = 42,
            EndTime = new DateTime(2026, 7, 20, 9, 0, 0),
            Tonnage = 1.25m
        });

        Assert.Empty(errors);
    }

    private static IReadOnlyList<ValidationResult> Validate(object input)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            errors,
            validateAllProperties: true);
        return errors;
    }
}
