namespace OcpProduction.Application.Drivers.Models;

public static class DriverStatusDefinitions
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string OnLeave = "OnLeave";

    public static readonly string[] All = [Active, Inactive, OnLeave];
}
