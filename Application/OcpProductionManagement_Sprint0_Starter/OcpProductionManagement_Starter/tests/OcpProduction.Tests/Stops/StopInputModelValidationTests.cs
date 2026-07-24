using System.ComponentModel.DataAnnotations;
using OcpProduction.Web.ViewModels.Stops;
using Xunit;

namespace OcpProduction.Tests.Stops;

public sealed class StopInputModelValidationTests
{
    [Fact]
    public void Create_input_requires_truck_reason_and_start_time()
    {
        var errors = Validate(new StopCreateInputModel());

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(StopCreateInputModel.TruckId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(StopCreateInputModel.StopReasonId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(StopCreateInputModel.StartTime)));
    }

    [Fact]
    public void Input_rejects_comments_longer_than_database_column()
    {
        var input = CreateValidInput();
        input.Comments = new string('x', 501);

        Assert.Contains(
            Validate(input),
            error => error.MemberNames.Contains(nameof(StopCreateInputModel.Comments)));
    }

    [Theory]
    [InlineData("   ", null)]
    [InlineData("  Observation sûre  ", "Observation sûre")]
    public void Input_normalizes_comments_safely(string source, string? expected)
    {
        var input = CreateValidInput();

        input.Comments = source;

        Assert.Equal(expected, input.Comments);
    }

    [Fact]
    public void Input_models_do_not_expose_protected_fields()
    {
        var propertyNames = typeof(StopCreateInputModel)
            .GetProperties()
            .Concat(typeof(StopEditInputModel).GetProperties())
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("EndTime", propertyNames);
        Assert.DoesNotContain("DurationMinutes", propertyNames);
        Assert.DoesNotContain("ValidationStatus", propertyNames);
        Assert.DoesNotContain("CreatedAt", propertyNames);
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

    private static StopCreateInputModel CreateValidInput() => new()
    {
        TruckId = 1,
        StopReasonId = 1,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0)
    };
}
