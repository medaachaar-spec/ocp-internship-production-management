using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_TripDetail
{
    public long TripID { get; set; }

    public DateOnly TripDate { get; set; }

    public int ShiftID { get; set; }

    public string ShiftCode { get; set; } = null!;

    public string ShiftName { get; set; } = null!;

    public int TruckID { get; set; }

    public string TruckCode { get; set; } = null!;

    public string? RegistrationNumber { get; set; }

    public int DriverID { get; set; }

    public string DriverCode { get; set; } = null!;

    public string DriverName { get; set; } = null!;

    public int LoadingPointID { get; set; }

    public string LoadingPointCode { get; set; } = null!;

    public string LoadingPointName { get; set; } = null!;

    public string? ZoneName { get; set; }

    public string? LoadingPointDescription { get; set; }

    public int DestinationID { get; set; }

    public string DestinationCode { get; set; } = null!;

    public string DestinationName { get; set; } = null!;

    public string? DestinationType { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public int IsLongTrip { get; set; }

    public decimal? Tonnage { get; set; }

    public string Status { get; set; } = null!;

    public string? Comments { get; set; }
}
