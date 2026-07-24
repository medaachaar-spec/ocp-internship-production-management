using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_GlobalProductionSummary
{
    public int? TotalTrips { get; set; }

    public int? CompletedTrips { get; set; }

    public int? IncompleteTrips { get; set; }

    public decimal? TotalTonnage { get; set; }

    public decimal? AverageTripDurationMinutes { get; set; }

    public int? LongTripCount { get; set; }

    public decimal? LongTripRate { get; set; }

    public int? TotalStops { get; set; }

    public int? TotalDowntimeMinutes { get; set; }

    public decimal? AverageStopDurationMinutes { get; set; }

    public int? NumberOfAnalyses { get; set; }

    public decimal? AverageP2O5 { get; set; }

    public decimal? MinimumP2O5 { get; set; }

    public decimal? MaximumP2O5 { get; set; }
}
