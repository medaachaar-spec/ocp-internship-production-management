using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using OcpProduction.Application.Dashboard.Models;
using OcpProduction.Web.ModelBinding;
using Xunit;

namespace OcpProduction.Tests.Dashboard;

public sealed class DashboardDateCriteriaModelBinderTests
{
    [Fact]
    public async Task Valid_french_dates_bind_exactly_and_preserve_submitted_values()
    {
        var context = await BindAsync("01/06/2026", "30/06/2026");

        var criteria = Assert.IsType<DashboardDateCriteria>(context.Result.Model);
        Assert.Equal(new DateOnly(2026, 6, 1), criteria.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 30), criteria.EndDate);
        Assert.Empty(
            context.ModelState.SelectMany(entry => entry.Value!.Errors));
        Assert.Equal("01/06/2026", context.ModelState["StartDate"]!.AttemptedValue);
        Assert.Equal("30/06/2026", context.ModelState["EndDate"]!.AttemptedValue);
    }

    [Theory]
    [InlineData("31/02/2026")]
    [InlineData("2026/06/01")]
    [InlineData("2026-06-30")]
    [InlineData("06/30/2026")]
    public async Task Invalid_non_french_dates_are_rejected_and_preserved(
        string submittedValue)
    {
        var context = await BindAsync(submittedValue, "30/06/2026");

        var criteria = Assert.IsType<DashboardDateCriteria>(context.Result.Model);
        Assert.Null(criteria.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 30), criteria.EndDate);
        Assert.False(context.ModelState.IsValid);
        Assert.Equal(submittedValue, context.ModelState["StartDate"]!.AttemptedValue);
        var error = Assert.Single(context.ModelState["StartDate"]!.Errors);
        Assert.Equal(DashboardDateCriteriaModelBinder.ValidationMessage, error.ErrorMessage);
    }

    [Fact]
    public async Task Empty_values_bind_as_nullable_dates_without_validation_errors()
    {
        var context = await BindAsync(string.Empty, string.Empty);

        var criteria = Assert.IsType<DashboardDateCriteria>(context.Result.Model);
        Assert.Null(criteria.StartDate);
        Assert.Null(criteria.EndDate);
        Assert.Empty(
            context.ModelState.SelectMany(entry => entry.Value!.Errors));
    }

    private static async Task<ModelBindingContext> BindAsync(
        string startDate,
        string endDate)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var valueProvider = new TestValueProvider(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["StartDate"] = startDate,
                ["EndDate"] = endDate
            });
        var metadataProvider = new EmptyModelMetadataProvider();
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            valueProvider,
            metadataProvider.GetMetadataForType(typeof(DashboardDateCriteria)),
            bindingInfo: null,
            modelName: string.Empty);

        await new DashboardDateCriteriaModelBinder().BindModelAsync(bindingContext);

        return bindingContext;
    }

    private sealed class TestValueProvider(
        IReadOnlyDictionary<string, string> values) : IValueProvider
    {
        public bool ContainsPrefix(string prefix)
        {
            return values.Keys.Any(
                key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        public ValueProviderResult GetValue(string key)
        {
            return values.TryGetValue(key, out var value)
                ? new ValueProviderResult(value, CultureInfo.InvariantCulture)
                : ValueProviderResult.None;
        }
    }
}
