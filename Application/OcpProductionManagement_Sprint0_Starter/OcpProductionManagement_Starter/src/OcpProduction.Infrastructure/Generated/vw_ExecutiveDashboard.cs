using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_ExecutiveDashboard
{
    public DateOnly? DataStartDate { get; set; }

    public DateOnly? DataEndDate { get; set; }

    public int? TotalCalendarDays { get; set; }

    public long? TotalActiveTripDays { get; set; }

    public long? TotalTrucks { get; set; }

    public long? ActiveTrucks { get; set; }

    public long? TotalDrivers { get; set; }

    public long? ActiveDrivers { get; set; }

    public long? TotalLoadingPoints { get; set; }

    public long? TotalDestinations { get; set; }

    public long? TotalTrips { get; set; }

    public long? CompletedTrips { get; set; }

    public long? IncompleteTrips { get; set; }

    public decimal? CompletionRatePercent { get; set; }

    public decimal? TotalTonnage { get; set; }

    public decimal? AveragePayloadTonnes { get; set; }

    public decimal? AveragePayloadUtilizationPercent { get; set; }

    public decimal? AverageTripDurationMinutes { get; set; }

    public int? MinimumTripDurationMinutes { get; set; }

    public int? MaximumTripDurationMinutes { get; set; }

    public long? TotalStops { get; set; }

    public long? StopsLinkedToTrips { get; set; }

    public long? StopsWithoutTrips { get; set; }

    public long? CompletedStops { get; set; }

    public long? OpenStops { get; set; }

    public long? TotalDowntimeMinutes { get; set; }

    public decimal? AverageStopDurationMinutes { get; set; }

    public int? MaximumStopDurationMinutes { get; set; }

    public long? TotalQualityResults { get; set; }

    public long? LinkedQualityResults { get; set; }

    public long? UnlinkedQualityResults { get; set; }

    public long? TotalP2O5Results { get; set; }

    public decimal? AverageP2O5 { get; set; }

    public decimal? MinimumP2O5 { get; set; }

    public decimal? MaximumP2O5 { get; set; }

    public long? TripsWithStops { get; set; }

    public long? TripsWithoutStops { get; set; }

    public long? TripsWithQualityResults { get; set; }

    public long? TripsWithoutQualityResults { get; set; }

    public long? TrucksWithTrips { get; set; }

    public long? DriversWithTrips { get; set; }

    public decimal? AverageTripsPerActiveDay { get; set; }

    public decimal? AverageTonnagePerActiveDay { get; set; }
}
