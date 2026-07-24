using OcpProduction.Web.ModelBinding;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class FlexibleDecimalModelBinderTests
{
    [Theory]
    [InlineData("1.25", 1.25)]
    [InlineData("1,25", 1.25)]
    [InlineData("99999999.99", 99999999.99)]
    public void TryParse_accepts_browser_and_French_decimal_separators(
        string submittedValue,
        decimal expected)
    {
        var parsed = FlexibleDecimalModelBinder.TryParse(submittedValue, out var value);

        Assert.True(parsed);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234.56")]
    [InlineData("1.234,56")]
    [InlineData("not-a-number")]
    public void TryParse_rejects_ambiguous_or_invalid_values(string submittedValue)
    {
        Assert.False(FlexibleDecimalModelBinder.TryParse(submittedValue, out _));
    }
}
