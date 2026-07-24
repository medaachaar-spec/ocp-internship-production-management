using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class Trip
{
    public long TripID { get; set; }

    public DateOnly TripDate { get; set; }

    public int ShiftID { get; set; }

    public int TruckID { get; set; }

    public int DriverID { get; set; }

    public int LoadingPointID { get; set; }

    public int DestinationID { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? Tonnage { get; set; }

    public string Status { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual Driver Driver { get; set; } = null!;

    public virtual LoadingPoint LoadingPoint { get; set; } = null!;

    public virtual ICollection<QualityResult> QualityResults { get; set; } = new List<QualityResult>();

    public virtual WorkShift Shift { get; set; } = null!;

    public virtual Truck Truck { get; set; } = null!;

    public virtual ICollection<TruckStop> TruckStops { get; set; } = new List<TruckStop>();
}
