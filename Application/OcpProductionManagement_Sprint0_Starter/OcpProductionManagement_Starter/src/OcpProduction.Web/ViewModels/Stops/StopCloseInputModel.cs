using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class StopCloseInputModel
{
    [HiddenInput]
    [Range(1, long.MaxValue, ErrorMessage = "L’identifiant de l’arrêt est invalide.")]
    public long StopId { get; set; }

    [Display(Name = "Date et heure de fin")]
    [Required(ErrorMessage = "L’heure de fin est obligatoire.")]
    public DateTime? EndTime { get; set; }
}
