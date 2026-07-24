using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class Truck
{
    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public string? RegistrationNumber { get; set; }

    public string? Model { get; set; }

    public decimal? RatedPayloadTonnes { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public virtual ICollection<TruckStop> TruckStops { get; set; } = new List<TruckStop>();
}
