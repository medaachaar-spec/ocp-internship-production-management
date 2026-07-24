using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class TruckStop
{
    public long TruckStopID { get; set; }

    public int TruckID { get; set; }

    public int? DriverID { get; set; }

    public int? ShiftID { get; set; }

    public long? TripID { get; set; }

    public int StopReasonID { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string ValidationStatus { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Driver? Driver { get; set; }

    public virtual WorkShift? Shift { get; set; }

    public virtual StopReason StopReason { get; set; } = null!;

    public virtual Trip? Trip { get; set; }

    public virtual Truck Truck { get; set; } = null!;
}
