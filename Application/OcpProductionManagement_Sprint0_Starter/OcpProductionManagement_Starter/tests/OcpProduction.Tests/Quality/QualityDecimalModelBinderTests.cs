using OcpProduction.Web.ModelBinding;
using Xunit;

namespace OcpProduction.Tests.Quality;

public sealed class QualityDecimalModelBinderTests
{
    [Theory]
    [InlineData("30.125", 30.125)]
    [InlineData("30,125", 30.125)]
    [InlineData("0", 0)]
    public void TryParse_accepts_browser_and_French_decimal_separators(
        string submittedValue,
        decimal expected)
    {
        var parsed = QualityDecimalModelBinder.TryParse(submittedValue, out var value);

        Assert.True(parsed);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234.56")]
    [InlineData("1.234,56")]
    [InlineData("not-a-number")]
    public void TryParse_rejects_ambiguous_or_invalid_values(string submittedValue)
    {
        Assert.False(QualityDecimalModelBinder.TryParse(submittedValue, out _));
    }
}
