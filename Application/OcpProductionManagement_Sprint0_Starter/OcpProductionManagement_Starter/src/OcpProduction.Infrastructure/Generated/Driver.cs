using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class Driver
{
    public int DriverID { get; set; }

    public string DriverCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? LicenseNumber { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public virtual ICollection<TruckStop> TruckStops { get; set; } = new List<TruckStop>();
}
