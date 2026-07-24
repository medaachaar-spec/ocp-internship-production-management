using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Stops;

public sealed class StopEditInputModel : StopInputModelBase
{
    [Range(1, long.MaxValue, ErrorMessage = "L’identifiant de l’arrêt est invalide.")]
    public long StopId { get; set; }
}
