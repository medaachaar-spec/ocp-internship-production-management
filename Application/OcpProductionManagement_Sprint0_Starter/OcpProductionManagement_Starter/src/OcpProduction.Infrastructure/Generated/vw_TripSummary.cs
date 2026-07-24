using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_TripSummary
{
    public long TripID { get; set; }

    public DateOnly TripDate { get; set; }

    public string Status { get; set; } = null!;

    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public string? TruckModel { get; set; }

    public decimal? RatedPayloadTonnes { get; set; }

    public int DriverID { get; set; }

    public string DriverCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int ShiftID { get; set; }

    public string ShiftCode { get; set; } = null!;

    public string ShiftName { get; set; } = null!;

    public int LoadingPointID { get; set; }

    public string LoadingPointCode { get; set; } = null!;

    public string LoadingPointName { get; set; } = null!;

    public int DestinationID { get; set; }

    public string DestinationCode { get; set; } = null!;

    public string DestinationName { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? TripDurationMinutes { get; set; }

    public decimal? Tonnage { get; set; }

    public decimal? PayloadUtilizationPercent { get; set; }

    public long? StopCount { get; set; }

    public long? TotalStopDurationMinutes { get; set; }

    public decimal? AverageStopDurationMinutes { get; set; }

    public int? MaximumStopDurationMinutes { get; set; }

    public bool? HasStops { get; set; }

    public long? QualityResultCount { get; set; }

    public decimal? AverageP2O5 { get; set; }

    public decimal? MinimumP2O5 { get; set; }

    public decimal? MaximumP2O5 { get; set; }

    public decimal? LatestP2O5 { get; set; }

    public bool? HasQualityResult { get; set; }

    public bool? IsCompleted { get; set; }

    public bool? IsIncomplete { get; set; }
}
