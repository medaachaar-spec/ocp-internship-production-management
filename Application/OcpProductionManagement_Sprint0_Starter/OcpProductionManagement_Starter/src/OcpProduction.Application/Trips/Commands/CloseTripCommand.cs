namespace OcpProduction.Application.Trips.Commands;

public sealed class CloseTripCommand
{
    public long TripId { get; init; }

    public DateTime EndTime { get; init; }

    public decimal Tonnage { get; init; }
}
