namespace OcpProduction.Application.Stops.Commands;

public sealed class CloseStopCommand
{
    public long StopId { get; init; }

    public DateTime EndTime { get; init; }
}
