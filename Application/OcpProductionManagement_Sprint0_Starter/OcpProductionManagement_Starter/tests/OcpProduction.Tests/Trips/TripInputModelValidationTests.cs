using System.ComponentModel.DataAnnotations;
using OcpProduction.Web.ViewModels.Trips;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripInputModelValidationTests
{
    [Fact]
    public void Create_input_requires_every_database_required_form_field()
    {
        var errors = Validate(new TripCreateInputModel());

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.TripDate)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.StartTime)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.TruckId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.DriverId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.WorkShiftId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.LoadingPointId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(TripCreateInputModel.DestinationId)));
    }

    [Fact]
    public void Edit_input_rejects_end_time_before_start_time()
    {
        var input = CreateValidEditInput();
        input.EndTime = input.StartTime!.Value.AddMinutes(-1);

        var error = Assert.Single(Validate(input), error =>
            error.MemberNames.Contains(nameof(TripEditInputModel.EndTime)));

        Assert.Contains("ne peuvent pas précéder", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000000)]
    public void Edit_input_rejects_tonnage_outside_decimal_10_2_business_range(decimal tonnage)
    {
        var input = CreateValidEditInput();
        input.Tonnage = tonnage;

        Assert.Contains(
            Validate(input),
            error => error.MemberNames.Contains(nameof(TripEditInputModel.Tonnage)));
    }

    [Fact]
    public void Edit_input_accepts_equal_start_and_end_times_and_null_tonnage()
    {
        var input = CreateValidEditInput();
        input.EndTime = input.StartTime;
        input.Tonnage = null;

        Assert.Empty(Validate(input));
    }

    [Fact]
    public void Input_rejects_observations_longer_than_database_column()
    {
        var input = CreateValidEditInput();
        input.Comments = new string('x', 501);

        Assert.Contains(
            Validate(input),
            error => error.MemberNames.Contains(nameof(TripEditInputModel.Comments)));
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

    private static TripEditInputModel CreateValidEditInput() => new()
    {
        TripId = 42,
        TripDate = new DateOnly(2026, 7, 20),
        WorkShiftId = 1,
        TruckId = 1,
        DriverId = 1,
        LoadingPointId = 1,
        DestinationId = 1,
        StartTime = new DateTime(2026, 7, 20, 8, 0, 0),
        EndTime = new DateTime(2026, 7, 20, 8, 15, 0),
        Tonnage = 1.25m,
        Comments = "Observation valide"
    };
}
