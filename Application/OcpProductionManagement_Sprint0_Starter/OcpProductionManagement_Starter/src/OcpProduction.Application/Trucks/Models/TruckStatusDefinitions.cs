namespace OcpProduction.Application.Trucks.Models;

public static class TruckStatusDefinitions
{
    public const string Available = "Available";
    public const string InService = "InService";
    public const string Maintenance = "Maintenance";
    public const string OutOfService = "OutOfService";

    public static readonly string[] All = [Available, InService, Maintenance, OutOfService];
}
