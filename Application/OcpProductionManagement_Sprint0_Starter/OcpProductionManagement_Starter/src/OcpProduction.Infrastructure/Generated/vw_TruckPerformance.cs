using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_TruckPerformance
{
    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public string? TruckModel { get; set; }

    public string TruckStatus { get; set; } = null!;

    public decimal? RatedPayloadTonnes { get; set; }

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

    public long? TotalStopCount { get; set; }

    public long? CompletedStopCount { get; set; }

    public long? TotalDowntimeMinutes { get; set; }

    public decimal? AverageStopDurationMinutes { get; set; }

    public int? MaximumStopDurationMinutes { get; set; }

    public long? StopsWithTripCount { get; set; }

    public long? StopsWithoutTripCount { get; set; }

    public bool? HasStops { get; set; }

    public long? QualityResultCount { get; set; }

    public decimal? AverageP2O5 { get; set; }

    public decimal? MinimumP2O5 { get; set; }

    public decimal? MaximumP2O5 { get; set; }

    public bool? HasQualityResults { get; set; }
}
