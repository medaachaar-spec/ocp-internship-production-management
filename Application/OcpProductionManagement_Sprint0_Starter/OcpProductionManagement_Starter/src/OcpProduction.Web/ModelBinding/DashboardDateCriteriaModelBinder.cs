using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OcpProduction.Application.Dashboard.Models;

namespace OcpProduction.Web.ModelBinding;

public sealed class DashboardDateCriteriaModelBinder : IModelBinder
{
    public const string DateFormat = "dd/MM/yyyy";
    public const string ValidationMessage =
        "La date doit respecter le format jj/mm/aaaa.";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var criteria = new DashboardDateCriteria
        {
            StartDate = BindDate(
                bindingContext,
                nameof(DashboardDateCriteria.StartDate)),
            EndDate = BindDate(
                bindingContext,
                nameof(DashboardDateCriteria.EndDate))
        };

        bindingContext.Result = ModelBindingResult.Success(criteria);
        return Task.CompletedTask;
    }

    public static bool TryParseDate(string? submittedValue, out DateOnly value)
    {
        return DateOnly.TryParseExact(
            submittedValue,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value);
    }

    private static DateOnly? BindDate(
        ModelBindingContext bindingContext,
        string fieldName)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(fieldName);
        if (valueResult == ValueProviderResult.None)
        {
            return null;
        }

        bindingContext.ModelState.SetModelValue(fieldName, valueResult);
        var submittedValue = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(submittedValue))
        {
            return null;
        }

        if (TryParseDate(submittedValue, out var parsedValue))
        {
            return parsedValue;
        }

        bindingContext.ModelState.TryAddModelError(
            fieldName,
            ValidationMessage);
        return null;
    }
}
