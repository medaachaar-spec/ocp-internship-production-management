namespace OcpProduction.Application.Stops.Dtos;

public sealed class StopDetailsDto
{
    public long StopId { get; init; }

    public long? TripId { get; init; }

    public int TruckId { get; init; }

    public required string TruckCode { get; init; }

    public string? TruckRegistrationNumber { get; init; }

    public string? TruckModel { get; init; }

    public int? DriverId { get; init; }

    public string? DriverCode { get; init; }

    public string? DriverName { get; init; }

    public int? WorkShiftId { get; init; }

    public string? WorkShiftCode { get; init; }

    public string? WorkShiftName { get; init; }

    public int StopReasonId { get; init; }

    public required string StopReasonCode { get; init; }

    public required string StopReasonName { get; init; }

    public string? StopReasonCategory { get; init; }

    public bool IsPlanned { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int? DurationMinutes { get; init; }

    public required string State { get; init; }

    public required string ValidationStatus { get; init; }

    public string? Observation { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? LoadingPointCode { get; init; }

    public string? LoadingPointName { get; init; }

    public string? DestinationCode { get; init; }

    public string? DestinationName { get; init; }
}
