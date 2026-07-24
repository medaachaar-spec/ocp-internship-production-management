namespace OcpProduction.Application.Security;

public static class FleetAccessRoles
{
    public const string All =
        AppRoles.AgentProduction + "," +
        AppRoles.Superviseur + "," +
        AppRoles.ResponsableProduction + "," +
        AppRoles.Administrateur;
}
