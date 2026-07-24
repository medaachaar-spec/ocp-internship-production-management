using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class Destination
{
    public int DestinationID { get; set; }

    public string DestinationCode { get; set; } = null!;

    public string DestinationName { get; set; } = null!;

    public string? DestinationType { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
