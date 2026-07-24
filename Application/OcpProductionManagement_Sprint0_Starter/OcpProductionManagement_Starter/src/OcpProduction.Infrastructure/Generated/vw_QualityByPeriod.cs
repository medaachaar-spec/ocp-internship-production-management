using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_QualityByPeriod
{
    public DateOnly? AnalysisDate { get; set; }

    public int? LoadingPointID { get; set; }

    public string? LoadingPointCode { get; set; }

    public string? LoadingPointName { get; set; }

    public string? ZoneName { get; set; }

    public string QualityUnit { get; set; } = null!;

    public int? NumberOfAnalyses { get; set; }

    public decimal? AverageQuality { get; set; }

    public decimal? MinimumQuality { get; set; }

    public decimal? MaximumQuality { get; set; }

    public int? InTargetCount { get; set; }

    public int? OutOfTargetOrReviewCount { get; set; }
}
