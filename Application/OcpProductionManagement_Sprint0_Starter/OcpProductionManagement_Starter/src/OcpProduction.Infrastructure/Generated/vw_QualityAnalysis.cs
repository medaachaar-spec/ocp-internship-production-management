using System;
using System.Collections.Generic;

namespace OcpProduction.Infrastructure.Generated;

public partial class vw_QualityAnalysis
{
    public long QualityResultID { get; set; }

    public long? TripID { get; set; }

    public int? LoadingPointID { get; set; }

    public DateTime AnalysisDate { get; set; }

    public DateOnly? QualityDate { get; set; }

    public string QualityUnit { get; set; } = null!;

    public decimal? P2O5Percent { get; set; }

    public decimal? TargetValue { get; set; }

    public string QualityStatus { get; set; } = null!;

    public bool? HasLinkedTrip { get; set; }

    public DateOnly? TripDate { get; set; }

    public string? TripStatus { get; set; }

    public int? TruckID { get; set; }

    public string? TruckCode { get; set; }

    public int? DriverID { get; set; }

    public string? DriverCode { get; set; }

    public string? DriverFullName { get; set; }

    public int? ShiftID { get; set; }

    public string? ShiftCode { get; set; }

    public string? ShiftName { get; set; }

    public string? LoadingPointCode { get; set; }

    public string? LoadingPointName { get; set; }

    public int? DestinationID { get; set; }

    public string? DestinationCode { get; set; }

    public string? DestinationName { get; set; }

    public bool? IsP2O5Measurement { get; set; }

    public bool? HasValidP2O5 { get; set; }

    public bool? LatestResultForTrip { get; set; }

    public long? AnalysisSequenceForTrip { get; set; }

    public long? ResultCountForTrip { get; set; }
}
