using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_StopsByReason
{
    public int StopReasonID { get; set; }

    public string ReasonCode { get; set; } = null!;

    public string ReasonName { get; set; } = null!;

    public string? ReasonCategory { get; set; }

    public bool IsPlanned { get; set; }

    public int? TotalStops { get; set; }

    public int? TotalDowntimeMinutes { get; set; }

    public decimal? AverageStopDurationMinutes { get; set; }
}
