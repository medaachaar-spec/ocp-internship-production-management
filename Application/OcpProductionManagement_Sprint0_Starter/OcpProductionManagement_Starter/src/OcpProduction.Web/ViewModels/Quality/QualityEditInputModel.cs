using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Quality;

public sealed class QualityEditInputModel : QualityInputModelBase
{
    [Range(0, long.MaxValue)]
    public long QualityResultId { get; set; }
}
