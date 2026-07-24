using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Quality;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class QualityMeasurementAttribute : ValidationAttribute
{
    private const decimal MaximumValue = 9_999_999.999m;

    public QualityMeasurementAttribute()
    {
        ErrorMessage =
            "La valeur P₂O₅ doit être positive ou nulle, comporter au maximum trois décimales et respecter le format decimal(10,3).";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is decimal measurement &&
            measurement >= 0 &&
            measurement <= MaximumValue &&
            decimal.Round(measurement, 3, MidpointRounding.ToEven) == measurement;
    }
}
