using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Web.ModelBinding;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class TripEditInputModel : TripInputModelBase, IValidatableObject
{
    [Range(1, long.MaxValue, ErrorMessage = "L’identifiant du voyage est invalide.")]
    public long TripId { get; set; }

    [Display(Name = "Date et heure de fin")]
    [DataType(DataType.DateTime)]
    public DateTime? EndTime { get; set; }

    [Display(Name = "Tonnage")]
    [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
    public decimal? Tonnage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime < StartTime)
        {
            yield return new ValidationResult(
                "La date et l’heure de fin ne peuvent pas précéder le début.",
                [nameof(EndTime)]);
        }

        if (Tonnage is <= 0 or > 99_999_999.99m)
        {
            yield return new ValidationResult(
                "Le tonnage doit être compris entre 0,01 et 99 999 999,99 tonnes.",
                [nameof(Tonnage)]);
        }
    }
}
