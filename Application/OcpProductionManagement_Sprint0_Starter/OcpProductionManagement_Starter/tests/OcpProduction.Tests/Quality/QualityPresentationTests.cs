using OcpProduction.Application.Quality.Models;
using OcpProduction.Web.ViewModels.Quality;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityPresentationTests
{
    [Fact]
    public void Presentation_formats_verified_dates_decimals_and_units_in_French()
    {
        Assert.Equal(
            "20/07/2026 14:05",
            QualityPresentation.FormatDate(new DateTime(2026, 7, 20, 14, 5, 0)));
        Assert.Equal("30,125", QualityPresentation.FormatDecimal(30.125m));
        Assert.Equal(
            "30,125 % P₂O₅",
            QualityPresentation.FormatMeasurement(30.125m, "P2O5Percent"));
    }

    [Fact]
    public void Presentation_uses_explicit_fallbacks_for_missing_values()
    {
        Assert.Equal("—", QualityPresentation.FormatDate(null));
        Assert.Equal("—", QualityPresentation.FormatDecimal(null));
        Assert.Equal("—", QualityPresentation.FormatMeasurement(null, "P2O5Percent"));
        Assert.Equal("—", QualityPresentation.OrFallback("   "));
    }

    [Theory]
    [InlineData(QualityStatusDefinitions.Pending, "En attente")]
    [InlineData(QualityStatusDefinitions.Review, "À examiner")]
    [InlineData(QualityStatusDefinitions.AboveTarget, "Au-dessus de la cible enregistrée")]
    [InlineData(QualityStatusDefinitions.BelowTarget, "En dessous de la cible enregistrée")]
    [InlineData(QualityStatusDefinitions.InTarget, "Dans la cible enregistrée")]
    public void Presentation_maps_only_actual_stored_statuses(string status, string expected)
    {
        var label = QualityPresentation.GetStatusLabel(status);

        Assert.Equal(expected, label);
        Assert.DoesNotContain("conforme", label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Presentation_preserves_an_unknown_status_without_inference()
    {
        Assert.Equal("FutureStatus", QualityPresentation.GetStatusLabel("FutureStatus"));
    }
}
