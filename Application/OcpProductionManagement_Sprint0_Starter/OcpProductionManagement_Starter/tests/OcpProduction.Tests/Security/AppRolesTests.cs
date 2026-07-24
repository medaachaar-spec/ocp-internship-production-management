using OcpProduction.Application.Security;
using Xunit;

namespace OcpProduction.Tests.Security;

public sealed class AppRolesTests
{
    [Fact]
    public void All_contains_the_five_MVP_roles()
    {
        Assert.Equal(5, AppRoles.All.Length);
        Assert.Contains(AppRoles.AgentProduction, AppRoles.All);
        Assert.Contains(AppRoles.AgentQualite, AppRoles.All);
        Assert.Contains(AppRoles.Superviseur, AppRoles.All);
        Assert.Contains(AppRoles.ResponsableProduction, AppRoles.All);
        Assert.Contains(AppRoles.Administrateur, AppRoles.All);
        Assert.Equal(AppRoles.All.Length, AppRoles.All.Distinct(StringComparer.Ordinal).Count());
    }
}
