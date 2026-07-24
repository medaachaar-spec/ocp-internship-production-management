using System.ComponentModel.DataAnnotations;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Web.ViewModels.Quality;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityInputModelValidationTests
{
    [Fact]
    public void Valid_input_accepts_a_verified_source_and_three_decimal_measurement()
    {
        var model = new QualityCreateInputModel
        {
            AnalysisDate = new DateTime(2026, 7, 20, 15, 0, 0),
            LoadingPointId = 1,
            QualityValue = 30.125m,
            Comments = "  Échantillon contrôlé  "
        };

        var errors = Validate(model);

        Assert.Empty(errors);
        Assert.Equal("Échantillon contrôlé", model.Comments);
    }

    [Fact]
    public void Input_requires_a_trip_or_loading_point()
    {
        var model = CreateValidModel();
        model.TripId = null;
        model.LoadingPointId = null;

        var error = Assert.Single(Validate(model));

        Assert.Contains(nameof(QualityInputModelBase.LoadingPointId), error.MemberNames);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(30.1234)]
    [InlineData(10000000)]
    public void Input_rejects_values_outside_decimal_10_3_or_SQL_constraint(decimal value)
    {
        var model = CreateValidModel();
        model.QualityValue = value;

        Assert.Contains(
            Validate(model),
            error => error.MemberNames.Contains(nameof(QualityInputModelBase.QualityValue)));
    }

    [Fact]
    public void Input_rejects_an_observation_longer_than_the_verified_column()
    {
        var model = CreateValidModel();
        model.Comments = new string('x', 501);

        Assert.Contains(
            Validate(model),
            error => error.MemberNames.Contains(nameof(QualityInputModelBase.Comments)));
    }

    [Theory]
    [InlineData(QualityStatusDefinitions.Pending)]
    [InlineData(QualityStatusDefinitions.Review)]
    [InlineData(QualityStatusDefinitions.AboveTarget)]
    [InlineData(QualityStatusDefinitions.BelowTarget)]
    [InlineData(QualityStatusDefinitions.InTarget)]
    public void Existing_analytical_statuses_do_not_invent_an_edit_lock(string status)
    {
        Assert.True(QualityEditability.IsEditable(status));
    }

    private static QualityCreateInputModel CreateValidModel() => new()
    {
        AnalysisDate = new DateTime(2026, 7, 20, 15, 0, 0),
        TripId = 42,
        QualityValue = 30.125m
    };

    private static IReadOnlyList<ValidationResult> Validate(QualityCreateInputModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
