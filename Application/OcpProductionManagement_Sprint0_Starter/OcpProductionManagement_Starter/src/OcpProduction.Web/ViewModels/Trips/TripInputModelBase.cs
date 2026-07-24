using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Trips;

public abstract class TripInputModelBase
{
    [Display(Name = "Date opérationnelle")]
    [Required(ErrorMessage = "La date opérationnelle est obligatoire.")]
    [DataType(DataType.Date)]
    public DateOnly? TripDate { get; set; }

    [Display(Name = "Poste")]
    [Required(ErrorMessage = "Le poste est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le poste est obligatoire.")]
    public int? WorkShiftId { get; set; }

    [Display(Name = "Camion")]
    [Required(ErrorMessage = "Le camion est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le camion est obligatoire.")]
    public int? TruckId { get; set; }

    [Display(Name = "Conducteur")]
    [Required(ErrorMessage = "Le conducteur est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le conducteur est obligatoire.")]
    public int? DriverId { get; set; }

    [Display(Name = "Point de chargement")]
    [Required(ErrorMessage = "Le point de chargement est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "Le point de chargement est obligatoire.")]
    public int? LoadingPointId { get; set; }

    [Display(Name = "Destination")]
    [Required(ErrorMessage = "La destination est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "La destination est obligatoire.")]
    public int? DestinationId { get; set; }

    [Display(Name = "Date et heure de début")]
    [Required(ErrorMessage = "La date et l’heure de début sont obligatoires.")]
    [DataType(DataType.DateTime)]
    public DateTime? StartTime { get; set; }

    [Display(Name = "Observation")]
    [StringLength(500, ErrorMessage = "L’observation ne peut pas dépasser 500 caractères.")]
    public string? Comments { get; set; }
}
