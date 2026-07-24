using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_DriverPerformance
{
    public int DriverID { get; set; }

    public string DriverCode { get; set; } = null!;

    public string DriverFullName { get; set; } = null!;

    public string DriverStatus { get; set; } = null!;

    public long? TotalTrips { get; set; }

    public long? CompletedTrips { get; set; }

    public long? IncompleteTrips { get; set; }

    public decimal? CompletionRatePercent { get; set; }

    public long? ActiveTripDays { get; set; }

    public DateOnly? FirstTripDate { get; set; }

    public DateOnly? LastTripDate { get; set; }

    public decimal? TripsPerActiveDay { get; set; }

    public bool? HasTrips { get; set; }

    public decimal? TotalTonnage { get; set; }

    public decimal? AveragePayloadTonnes { get; set; }

    public decimal? MinimumPayloadTonnes { get; set; }

    public decimal? MaximumPayloadTonnes { get; set; }

    public decimal? AveragePayloadUtilizationPercent { get; set; }

    public decimal? AverageTripDurationMinutes { get; set; }

    public int? MinimumTripDurationMinutes { get; set; }

    public int? MaximumTripDurationMinutes { get; set; }

    public long? TripsWithStops { get; set; }

    public long? TotalLinkedStopCount { get; set; }

    public long? TotalLinkedStopDurationMinutes { get; set; }

    public decimal? AverageLinkedStopDurationMinutes { get; set; }

    public int? MaximumLinkedStopDurationMinutes { get; set; }

    public bool? HasLinkedStops { get; set; }

    public long? QualityResultCount { get; set; }

    public decimal? AverageP2O5 { get; set; }

    public decimal? MinimumP2O5 { get; set; }

    public decimal? MaximumP2O5 { get; set; }

    public bool? HasQualityResults { get; set; }
}
