using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OcpProduction.Application.Security;
using OcpProduction.Web.Security;
using Xunit;

namespace OcpProduction.Tests.Security;

public sealed class AuthorizationPoliciesTests
{
    [Fact]
    public async Task Registered_policies_authorize_only_the_documented_roles()
    {
        IReadOnlyDictionary<string, string[]> expectedRoles =
            new Dictionary<string, string[]>
            {
                [PolicyNames.CanManageTrips] = [AppRoles.AgentProduction, AppRoles.Superviseur],
                [PolicyNames.CanManageStops] = [AppRoles.AgentProduction, AppRoles.Superviseur],
                [PolicyNames.CanManageQuality] = [AppRoles.AgentQualite, AppRoles.Superviseur],
                [PolicyNames.CanValidate] = [AppRoles.Superviseur],
                [PolicyNames.CanViewDashboard] =
                    [AppRoles.Superviseur, AppRoles.ResponsableProduction, AppRoles.Administrateur],
                [PolicyNames.CanViewReporting] = [AppRoles.Superviseur, AppRoles.ResponsableProduction],
                [PolicyNames.CanAdminister] = [AppRoles.Administrateur]
            };

        Assert.Equal(nameof(PolicyNames.CanViewDashboard), PolicyNames.CanViewDashboard);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOcpAuthorization();

        await using var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        foreach (var (policy, allowedRoles) in expectedRoles)
        {
            foreach (var role in AppRoles.All)
            {
                var principal = CreatePrincipal(role);
                var result = await authorizationService.AuthorizeAsync(principal, null, policy);

                Assert.Equal(allowedRoles.Contains(role), result.Succeeded);
            }

            var anonymousResult = await authorizationService.AuthorizeAsync(
                new ClaimsPrincipal(new ClaimsIdentity()),
                null,
                policy);

            Assert.False(anonymousResult.Succeeded);
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
