using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_TripsDailySummary
{
    public DateOnly TripDate { get; set; }

    public int? TotalTrips { get; set; }

    public int? CompletedTrips { get; set; }

    public int? IncompleteTrips { get; set; }

    public decimal? TotalTonnage { get; set; }

    public decimal? AverageTripDurationMinutes { get; set; }

    public int? LongTripCount { get; set; }

    public decimal? LongTripRate { get; set; }
}
