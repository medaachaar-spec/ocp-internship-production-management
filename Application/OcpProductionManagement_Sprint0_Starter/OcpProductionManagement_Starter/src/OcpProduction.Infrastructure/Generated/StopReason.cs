using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class StopReason
{
    public int StopReasonID { get; set; }

    public string ReasonCode { get; set; } = null!;

    public string ReasonName { get; set; } = null!;

    public string? Category { get; set; }

    public bool IsPlanned { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<TruckStop> TruckStops { get; set; } = new List<TruckStop>();
}
