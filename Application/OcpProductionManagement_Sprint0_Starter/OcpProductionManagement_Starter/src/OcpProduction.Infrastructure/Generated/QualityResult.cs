using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class QualityResult
{
    public long QualityResultID { get; set; }

    public DateTime AnalysisDate { get; set; }

    public long? TripID { get; set; }

    public int? LoadingPointID { get; set; }

    public decimal QualityValue { get; set; }

    public string QualityUnit { get; set; } = null!;

    public decimal? TargetValue { get; set; }

    public string Status { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual LoadingPoint? LoadingPoint { get; set; }

    public virtual Trip? Trip { get; set; }
}
