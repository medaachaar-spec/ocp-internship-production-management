using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OcpProduction.Web.ModelBinding;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    private const NumberStyles AcceptedStyles =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var submittedValue = valueResult.FirstValue;

        if (string.IsNullOrWhiteSpace(submittedValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (TryParse(submittedValue, out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "Le tonnage doit être un nombre valide avec deux décimales au maximum.");
        return Task.CompletedTask;
    }

    public static bool TryParse(string submittedValue, out decimal value)
    {
        var culture = submittedValue.Contains(',', StringComparison.Ordinal)
            ? CultureInfo.GetCultureInfo("fr-FR")
            : CultureInfo.InvariantCulture;

        return decimal.TryParse(submittedValue, AcceptedStyles, culture, out value);
    }
}
