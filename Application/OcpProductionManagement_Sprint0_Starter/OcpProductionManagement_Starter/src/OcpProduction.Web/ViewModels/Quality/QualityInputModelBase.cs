using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OcpProduction.Web.ModelBinding;

namespace OcpProduction.Web.ViewModels.Quality;

public abstract class QualityInputModelBase : IValidatableObject
{
    private string? _comments;

    [Display(Name = "Date et heure d’analyse")]
    [Required(ErrorMessage = "La date et l’heure d’analyse sont obligatoires.")]
    [DataType(DataType.DateTime)]
    public DateTime? AnalysisDate { get; set; }

    [Display(Name = "Voyage associé")]
    [Range(0, long.MaxValue, ErrorMessage = "L’identifiant du voyage est invalide.")]
    public long? TripId { get; set; }

    [Display(Name = "Point de chargement")]
    [Range(1, int.MaxValue, ErrorMessage = "Le point de chargement sélectionné est invalide.")]
    public int? LoadingPointId { get; set; }

    [Display(Name = "Valeur P₂O₅")]
    [Required(ErrorMessage = "La valeur P₂O₅ est obligatoire.")]
    [QualityMeasurement]
    [ModelBinder(BinderType = typeof(QualityDecimalModelBinder))]
    public decimal? QualityValue { get; set; }

    [Display(Name = "Observation")]
    [StringLength(500, ErrorMessage = "L’observation ne peut pas dépasser 500 caractères.")]
    public string? Comments
    {
        get => _comments;
        set => _comments = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TripId.HasValue && !LoadingPointId.HasValue)
        {
            yield return new ValidationResult(
                "Indiquez un voyage ou un point de chargement.",
                [nameof(LoadingPointId)]);
        }
    }
}
