using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class WorkShift
{
    public int ShiftID { get; set; }

    public string ShiftCode { get; set; } = null!;

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public virtual ICollection<TruckStop> TruckStops { get; set; } = new List<TruckStop>();
}
