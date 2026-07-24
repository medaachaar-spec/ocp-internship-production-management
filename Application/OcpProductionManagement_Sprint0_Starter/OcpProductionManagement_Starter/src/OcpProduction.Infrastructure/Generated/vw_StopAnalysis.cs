using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_StopAnalysis
{
    public long TruckStopID { get; set; }

    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public long? TripID { get; set; }

    public int StopReasonID { get; set; }

    public string StopReasonCode { get; set; } = null!;

    public string StopReasonName { get; set; } = null!;

    public string? StopCategory { get; set; }

    public bool IsPlanned { get; set; }

    public string? PlannedUnplannedLabel { get; set; }

    public bool? HasLinkedTrip { get; set; }

    public string StopStatus { get; set; } = null!;

    public DateTime StopStartTime { get; set; }

    public DateTime? StopEndTime { get; set; }

    public DateOnly? StopDate { get; set; }

    public int? StopYear { get; set; }

    public int? StopMonthNumber { get; set; }

    public int? StopDurationMinutes { get; set; }

    public bool? IsCompletedStop { get; set; }

    public bool? IsOpenStop { get; set; }

    public DateOnly? TripDate { get; set; }

    public int? DriverID { get; set; }

    public string? DriverCode { get; set; }

    public string? DriverFullName { get; set; }

    public int? ShiftID { get; set; }

    public string? ShiftCode { get; set; }

    public string? ShiftName { get; set; }

    public int? LoadingPointID { get; set; }

    public string? LoadingPointCode { get; set; }

    public string? LoadingPointName { get; set; }

    public int? DestinationID { get; set; }

    public string? DestinationCode { get; set; }

    public string? DestinationName { get; set; }

    public string? TripStatus { get; set; }
}
