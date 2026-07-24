using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class LoadingPoint
{
    public int LoadingPointID { get; set; }

    public string LoadingPointCode { get; set; } = null!;

    public string LoadingPointName { get; set; } = null!;

    public string? ZoneName { get; set; }

    public string? LoadingPointDescription { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<QualityResult> QualityResults { get; set; } = new List<QualityResult>();

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
