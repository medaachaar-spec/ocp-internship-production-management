using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Stops;

public abstract class StopInputModelBase
{
    private string? _comments;

    [Display(Name = "Voyage associé")]
    [Range(1, long.MaxValue, ErrorMessage = "L’identifiant du voyage est invalide.")]
    public long? TripId { get; set; }

    [Display(Name = "Camion")]
    [Required(ErrorMessage = "Le camion est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le camion est obligatoire.")]
    public int? TruckId { get; set; }

    [Display(Name = "Conducteur")]
    [Range(1, int.MaxValue, ErrorMessage = "Le conducteur sélectionné est invalide.")]
    public int? DriverId { get; set; }

    [Display(Name = "Poste")]
    [Range(1, int.MaxValue, ErrorMessage = "Le poste sélectionné est invalide.")]
    public int? WorkShiftId { get; set; }

    [Display(Name = "Motif d’arrêt")]
    [Required(ErrorMessage = "Le motif d’arrêt est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le motif d’arrêt est obligatoire.")]
    public int? StopReasonId { get; set; }

    [Display(Name = "Date et heure de début")]
    [Required(ErrorMessage = "La date et l’heure de début sont obligatoires.")]
    [DataType(DataType.DateTime)]
    public DateTime? StartTime { get; set; }

    [Display(Name = "Observation")]
    [StringLength(500, ErrorMessage = "L’observation ne peut pas dépasser 500 caractères.")]
    public string? Comments
    {
        get => _comments;
        set => _comments = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
