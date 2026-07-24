using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_TruckStopDetail
{
    public long TruckStopID { get; set; }

    public DateOnly? StopDate { get; set; }

    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public int? DriverID { get; set; }

    public string? DriverCode { get; set; }

    public string? DriverName { get; set; }

    public int? ShiftID { get; set; }

    public string? ShiftCode { get; set; }

    public string? ShiftName { get; set; }

    public long? TripID { get; set; }

    public int StopReasonID { get; set; }

    public string ReasonCode { get; set; } = null!;

    public string ReasonName { get; set; } = null!;

    public string? ReasonCategory { get; set; }

    public bool IsPlanned { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string ValidationStatus { get; set; } = null!;

    public string? Comments { get; set; }
}
