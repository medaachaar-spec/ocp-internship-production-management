using Microsoft.AspNetCore.Authorization;
using OcpProduction.Application.Security;

namespace OcpProduction.Web.Security;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddOcpAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(ConfigurePolicies);
        return services;
    }

    private static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(PolicyNames.CanManageTrips, policy =>
            policy.RequireRole(AppRoles.AgentProduction, AppRoles.Superviseur));
        options.AddPolicy(PolicyNames.CanManageStops, policy =>
            policy.RequireRole(AppRoles.AgentProduction, AppRoles.Superviseur));
        options.AddPolicy(PolicyNames.CanManageQuality, policy =>
            policy.RequireRole(AppRoles.AgentQualite, AppRoles.Superviseur));
        options.AddPolicy(PolicyNames.CanValidate, policy =>
            policy.RequireRole(AppRoles.Superviseur));
        options.AddPolicy(PolicyNames.CanViewDashboard, policy =>
            policy.RequireRole(
                AppRoles.Superviseur,
                AppRoles.ResponsableProduction,
                AppRoles.Administrateur));
        options.AddPolicy(PolicyNames.CanViewReporting, policy =>
            policy.RequireRole(AppRoles.Superviseur, AppRoles.ResponsableProduction));
        options.AddPolicy(PolicyNames.CanAdminister, policy =>
            policy.RequireRole(AppRoles.Administrateur));
    }
}
