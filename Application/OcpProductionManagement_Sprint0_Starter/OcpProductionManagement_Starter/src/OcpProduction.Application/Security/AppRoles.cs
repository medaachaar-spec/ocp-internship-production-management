namespace OcpProduction.Application.Security;

public static class AppRoles
{
    public const string AgentProduction = "AgentProduction";
    public const string AgentQualite = "AgentQualite";
    public const string Superviseur = "Superviseur";
    public const string ResponsableProduction = "ResponsableProduction";
    public const string Administrateur = "Administrateur";

    public static readonly string[] All =
    [
        AgentProduction,
        AgentQualite,
        Superviseur,
        ResponsableProduction,
        Administrateur
    ];
}
