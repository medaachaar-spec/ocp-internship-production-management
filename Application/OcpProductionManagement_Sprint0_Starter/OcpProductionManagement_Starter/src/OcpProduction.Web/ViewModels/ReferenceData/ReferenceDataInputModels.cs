using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.ReferenceData;

public sealed class WorkShiftInputModel
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required]
    public TimeOnly? EndTime { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class LoadingPointInputModel
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ZoneName { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class DestinationInputModel
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Type { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class StopReasonInputModel
{
    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Category { get; set; }

    public bool IsPlanned { get; set; }
    public bool IsActive { get; set; } = true;
}
