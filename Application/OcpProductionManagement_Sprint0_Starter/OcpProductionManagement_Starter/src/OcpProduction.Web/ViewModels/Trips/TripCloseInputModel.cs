using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Web.ModelBinding;

namespace OcpProduction.Web.ViewModels.Trips;

public sealed class TripCloseInputModel : IValidatableObject
{
    [HiddenInput]
    [Range(1, long.MaxValue, ErrorMessage = "L’identifiant du voyage est invalide.")]
    public long TripId { get; set; }

    [Display(Name = "Date et heure de fin")]
    [Required(ErrorMessage = "L’heure de fin est obligatoire.")]
    public DateTime? EndTime { get; set; }

    [Display(Name = "Tonnage")]
    [Required(ErrorMessage = "Le tonnage est obligatoire.")]
    [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
    public decimal? Tonnage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tonnage is <= 0 or > 99_999_999.99m)
        {
            yield return new ValidationResult(
                "Le tonnage doit être strictement positif.",
                [nameof(Tonnage)]);
        }
    }
}
